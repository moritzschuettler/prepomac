// PrePoMax for macOS - renders a render window into an image with an OpenGL context of its own (CGL), for the pictures and
// videos of the view and for the tests that run without a window.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Kitware.VTK.Gl
{
    internal static class OffscreenRenderer
    {
        private const string OpenGLFramework = "/System/Library/Frameworks/OpenGL.framework/OpenGL";
        [DllImport(OpenGLFramework)] private static extern int CGLChoosePixelFormat(int[] attribs, out IntPtr pix, out int npix);
        [DllImport(OpenGLFramework)] private static extern int CGLCreateContext(IntPtr pix, IntPtr share, out IntPtr ctx);
        [DllImport(OpenGLFramework)] private static extern int CGLDestroyPixelFormat(IntPtr pix);
        [DllImport(OpenGLFramework)] private static extern IntPtr CGLGetCurrentContext();
        [DllImport(OpenGLFramework)] private static extern int CGLSetCurrentContext(IntPtr ctx);

        private static IntPtr _context;
        private static IntPtr _library;
        private static GL _gl;
        private static readonly Dictionary<vtkRenderWindow, SceneRenderer> _scenes = new Dictionary<vtkRenderWindow, SceneRenderer>();
        private static uint _fbo, _color;
        private static int _width, _height;

        private static void EnsureContext()
        {
            if (_context != IntPtr.Zero) return;
            int[] attributes = { 99, 0x4100, 73, 8, 24, 11, 8, 12, 24, 96, 0 };   // core 4.1, accelerated, color, alpha, depth
            if (CGLChoosePixelFormat(attributes, out IntPtr pix, out int n) != 0 || pix == IntPtr.Zero)
                throw new InvalidOperationException("No OpenGL pixel format for offscreen rendering.");
            int error = CGLCreateContext(pix, IntPtr.Zero, out _context);
            CGLDestroyPixelFormat(pix);
            if (error != 0) throw new InvalidOperationException("The offscreen OpenGL context could not be created.");
            _library = NativeLibrary.Load(OpenGLFramework);
            IntPtr previous = CGLGetCurrentContext();
            CGLSetCurrentContext(_context);
            _gl = new GL(name => NativeLibrary.TryGetExport(_library, name, out IntPtr p) ? p : IntPtr.Zero);
            CGLSetCurrentContext(previous);
        }
        // RGB or RGBA bytes of the window, bottom row first
        public static byte[] Render(vtkRenderWindow window, int width, int height, double scale, bool alpha)
        {
            EnsureContext();
            IntPtr previous = CGLGetCurrentContext();
            CGLSetCurrentContext(_context);
            try
            {
                if (!_scenes.TryGetValue(window, out SceneRenderer scene))
                {
                    scene = new SceneRenderer();
                    scene.Initialize(_gl);
                    _scenes[window] = scene;
                }
                if (_fbo == 0 || _width != width || _height != height)
                {
                    _gl.DeleteFramebuffer(_fbo);
                    _gl.DeleteTexture(_color);
                    _fbo = _gl.GenFramebuffer();
                    _gl.BindFramebuffer(GL.FRAMEBUFFER, _fbo);
                    _color = _gl.GenTexture();
                    _gl.BindTexture(GL.TEXTURE_2D, _color);
                    _gl.TexImage2D(GL.TEXTURE_2D, 0, GL.RGBA8, width, height, GL.RGBA, GL.UNSIGNED_BYTE, null);
                    _gl.FramebufferTexture2D(GL.FRAMEBUFFER, GL.COLOR_ATTACHMENT0, GL.TEXTURE_2D, _color, 0);
                    _width = width;
                    _height = height;
                }
                scene.Render(window, _fbo, width, height, scale);
                _gl.BindFramebuffer(GL.FRAMEBUFFER, _fbo);
                _gl.PixelStorei(GL.PACK_ALIGNMENT, 1);
                byte[] rgba = new byte[width * height * 4];
                _gl.ReadPixels(0, 0, width, height, GL.RGBA, GL.UNSIGNED_BYTE, rgba);
                _gl.Finish();
                if (alpha) return rgba;
                byte[] rgb = new byte[width * height * 3];
                for (int i = 0, j = 0; i < rgba.Length; i += 4, j += 3)
                {
                    rgb[j] = rgba[i]; rgb[j + 1] = rgba[i + 1]; rgb[j + 2] = rgba[i + 2];
                }
                return rgb;
            }
            finally
            {
                CGLSetCurrentContext(previous);
            }
        }
    }
}
