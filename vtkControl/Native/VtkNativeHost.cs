// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
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


        // Events
        public event EventHandler ViewCreated;


        // Properties
        public VtkNativeView View { get { return _view; } }


        // Constructors
        public VtkNativeHost()
        {
            SetStyle(ControlStyles.Opaque | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        }


        // Methods
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
                _view.SetSize(ClientSize.Width, ClientSize.Height);
                _view.Render();
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_view != null) _view.Render();
        }
    }
}
