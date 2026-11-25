// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace vtkControl
{
    partial class vtkControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (components != null)
                    {
                        components.Dispose();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern System.IntPtr SetFocus(System.IntPtr hWnd);

        /// <summary>
        /// OnGotFocus fires after Windows keyboard focus enters the control.
        /// </summary>
        protected override void OnGotFocus(System.EventArgs e)
        {
            // The OpenGL window of the native view takes the keyboard, the arrow keys among it
            System.IntPtr hWnd = _nativeHost.ViewWindow;
            if (System.IntPtr.Zero != hWnd) SetFocus(hWnd);
            //
            base.OnGotFocus(e);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._nativeHost = new global::vtkControl.Native.VtkNativeHost();
            this.SuspendLayout();
            //
            // _nativeHost
            //
            this._nativeHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nativeHost.Location = new System.Drawing.Point(0, 0);
            this._nativeHost.Name = "_nativeHost";
            this._nativeHost.Size = new System.Drawing.Size(475, 326);
            this._nativeHost.TabIndex = 0;
            //
            // vtkControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._nativeHost);
            this.Name = "vtkControl";
            this.Size = new System.Drawing.Size(475, 326);
            this.Load += new System.EventHandler(this.vtkControl_Load);
            this.EnabledChanged += new System.EventHandler(this.vtkControl_EnabledChanged);
            this.Resize += new System.EventHandler(this.vtkControl_Resize);
            this.ResumeLayout(false);

        }

        #endregion

        private global::vtkControl.Native.VtkNativeHost _nativeHost;
    }
}
