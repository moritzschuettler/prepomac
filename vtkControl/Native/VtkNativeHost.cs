// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace vtkControl.Native
{
    // A window that a native view renders into: VTK creates its OpenGL window as a child of this one through
    // SetParentId and receives the mouse and keyboard messages there. The view exists while the handle does, so
    // nothing native is loaded until the host is first shown
    public class VtkNativeHost : Control
    {
        // Variables
        private VtkNativeView _view;
        private readonly Timer _resizeSettledTimer;


        // Events
        public event EventHandler ViewCreated;


        // Properties
        public VtkNativeView View { get { return _view; } }
        // The OpenGL window VTK created in this one, which takes the keyboard
        public IntPtr ViewWindow { get { return IsHandleCreated ? GetWindow(Handle, GW_CHILD) : IntPtr.Zero; } }


        // Constructors
        public VtkNativeHost()
        {
            SetStyle(ControlStyles.Opaque | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            _resizeSettledTimer = new Timer();
            _resizeSettledTimer.Interval = 300;
            _resizeSettledTimer.Tick += ResizeSettledTimer_Tick;
        }


        // Methods
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint command);
        private const uint GW_CHILD = 5;
        private const int WM_PARENTNOTIFY = 0x0210;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;
        //
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (DesignMode) return;
            //
            _view = VtkNativeView.CreateEmbedded(Handle, Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height));
            ViewCreated?.Invoke(this, EventArgs.Empty);
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            _resizeSettledTimer.Stop();
            if (_view != null)
            {
                _view.Dispose();
                _view = null;
            }
            base.OnHandleDestroyed(e);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_view != null && ClientSize.Width > 0 && ClientSize.Height > 0)
            {
                // The ambient occlusion and the depth sorting allocate their framebuffers, several times the size of
                // the window, again at every size, which takes far longer than the render itself; the window is drawn
                // without them until its size settles
                _view.SetBasePassSuspended(true);
                _view.SetSize(ClientSize.Width, ClientSize.Height);
                _resizeSettledTimer.Stop();
                _resizeSettledTimer.Start();
                // A drag raises many resize events; the paint they invalidate is merged into one render of the model
                Invalidate();
            }
        }
        private void ResizeSettledTimer_Tick(object sender, EventArgs e)
        {
            _resizeSettledTimer.Stop();
            if (_view == null) return;
            //
            _view.SetBasePassSuspended(false);
            Invalidate();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) _resizeSettledTimer.Dispose();
            base.Dispose(disposing);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_view != null) _view.Render();
        }
        // A click on the ActiViz view was a click on vtkControl, which took the focus of the form; a click on the
        // OpenGL window passes the form by, which kept the control focused before, a tool strip or the search box of the
        // model tree, and so ignored the keys of the view (the space to hide and show). The control takes the focus
        // here, as it did, and hands it on to the OpenGL window
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_PARENTNOTIFY)
            {
                int message = (int)(m.WParam.ToInt64() & 0xFFFF);
                if ((message == WM_LBUTTONDOWN || message == WM_RBUTTONDOWN || message == WM_MBUTTONDOWN) &&
                    Parent != null && !IsActiveControlOfForm(Parent)) Parent.Focus();
            }
            base.WndProc(ref m);
        }
        private bool IsActiveControlOfForm(Control control)
        {
            Control active = FindForm()?.ActiveControl;
            while (active != null)
            {
                if (active == control || active == this) return true;
                active = (active as ContainerControl)?.ActiveControl;
            }
            return false;
        }
    }
}
