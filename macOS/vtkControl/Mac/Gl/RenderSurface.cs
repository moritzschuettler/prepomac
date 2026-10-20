// PrePoMax for macOS - the OpenGL view of a render window inside the WinForms control the window was attached to.

using System;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;

namespace Kitware.VTK.Gl
{
    internal class RenderSurface : OpenGlControlBase
    {
        private readonly System.Windows.Forms.Control _control;
        private readonly vtkRenderWindow _window;
        private bool _failed;

        private RenderSurface(System.Windows.Forms.Control control, vtkRenderWindow window)
        {
            _control = control;
            _window = window;
        }
        public static RenderSurface Attach(System.Windows.Forms.Control control, vtkRenderWindow window)
        {
            RenderSurface surface = new RenderSurface(control, window);
            control.SetNativeVisual(surface);
            return surface;
        }
        public void RequestFrame()
        {
            if (Dispatcher.UIThread.CheckAccess()) RequestNextFrameRendering();
            else Dispatcher.UIThread.Post(RequestNextFrameRendering);
        }
        public void SetCursor(int cursor)
        {
            // 9 is the hand of VTK
            _control.Cursor = cursor == 9 ? System.Windows.Forms.Cursors.Hand : System.Windows.Forms.Cursors.Default;
        }
        protected override void OnOpenGlInit(GlInterface gl)
        {
            try
            {
                _window.Scene = new SceneRenderer();
                _window.Scene.Initialize(new GL(name => gl.GetProcAddress(name)));
                _failed = false;
            }
            catch (Exception ex)
            {
                _failed = true;
                Console.Error.WriteLine("The 3D view could not be initialized: " + ex);
            }
        }
        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            _window.Scene = new SceneRenderer();
        }
        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            if (_failed || !_window.Scene.Initialized) return;
            double scaling = VisualRoot?.RenderScaling ?? 1;
            int logicalWidth = Math.Max(1, (int)Math.Round(Bounds.Width));
            int logicalHeight = Math.Max(1, (int)Math.Round(Bounds.Height));
            _window.SetSize(logicalWidth, logicalHeight);
            int width = Math.Max(1, (int)Math.Round(Bounds.Width * scaling));
            int height = Math.Max(1, (int)Math.Round(Bounds.Height * scaling));
            _window.Scene.Render(_window, (uint)fb, width, height, (double)width / logicalWidth);
        }
    }
}
