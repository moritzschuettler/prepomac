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
using System.Threading;

namespace vtkControl.Native
{
    // A native render window with one renderer. It belongs to the thread that created it: its OpenGL context is
    // current there, so every call and the Dispose must come from that thread. There is deliberately no finalizer,
    // since a finalizer runs on another thread and would release the context from the wrong one
    public sealed partial class VtkNativeView : IDisposable
    {
        // Variables
        private IntPtr _handle;
        private readonly int _threadId;
        private readonly bool _offScreen;
        private VtkNativeMethods.EventCallback _eventCallback;


        // Events
        public event EventHandler<VtkNativeEventArgs> NativeEvent;


        // Properties
        public bool IsDisposed { get { return _handle == IntPtr.Zero; } }
        public bool OffScreen { get { return _offScreen; } }


        // Constructors
        private VtkNativeView(bool offScreen)
        {
            VtkNativeLibrary.EnsureLoaded();
            //
            IntPtr handle;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_create(offScreen ? 1 : 0, out handle));
            _handle = handle;
            _threadId = Thread.CurrentThread.ManagedThreadId;
            _offScreen = offScreen;
            // The delegate is kept in a field for as long as the native side holds its pointer
            _eventCallback = OnNativeEvent;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_event_callback(_handle, _eventCallback, IntPtr.Zero));
        }


        // Static methods
        public static VtkNativeView CreateOffScreen(int width, int height)
        {
            VtkNativeView view = new VtkNativeView(true);
            try
            {
                view.SetSize(width, height);
                return view;
            }
            catch
            {
                view.Dispose();
                throw;
            }
        }
        public static VtkNativeView CreateEmbedded(IntPtr parentWindow, int width, int height)
        {
            VtkNativeView view = new VtkNativeView(false);
            try
            {
                view.SetSize(width, height);
                VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_parent_window(view.Handle(), parentWindow));
                return view;
            }
            catch
            {
                view.Dispose();
                throw;
            }
        }


        // Methods
        public void SetSize(int width, int height)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_size(Handle(), width, height));
        }
        public void GetSize(out int width, out int height)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_get_size(Handle(), out width, out height));
        }
        public void SetBackground(double[] topRgb, double[] bottomRgb)
        {
            SetBackground(true, topRgb, bottomRgb);
        }
        public void SetBackground(bool gradient, double[] topRgb, double[] bottomRgb)
        {
            CheckLength(topRgb, 3, "topRgb");
            CheckLength(bottomRgb, 3, "bottomRgb");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_background(Handle(), gradient ? 1 : 0, topRgb,
                                                                              bottomRgb));
        }
        public void SetSmoothing(bool pointSmoothing, bool lineSmoothing)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_smoothing(Handle(), pointSmoothing ? 1 : 0,
                                                                             lineSmoothing ? 1 : 0));
        }
        public void Render()
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_render(Handle()));
        }
        public string GetRenderInfo()
        {
            IntPtr handle = Handle();
            return VtkNativeLibrary.GetString((byte[] buffer, int capacity, out int required) =>
                VtkNativeMethods.vtkn_view_get_render_info(handle, buffer, capacity, out required));
        }
        //
        public double[] GetCamera()
        {
            double[] values = new double[10];
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_get_camera(Handle(), values));
            return values;
        }
        public void SetCamera(double[] values)
        {
            CheckLength(values, 10, "values");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_camera(Handle(), values));
        }
        public void ResetCamera()
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_reset_camera(Handle()));
        }
        // Views: each ends as vtkControl ends a view change, with the camera distance, the clipping range and a render
        public void SetViewParameters(double[] values)
        {
            CheckLength(values, 10, "values");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_view_parameters(Handle(), values));
        }
        public void SetStandardView(VtkNativeOrientation orientation, bool zoomToFit)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_standard_view(Handle(), (int)orientation,
                                                                                 zoomToFit ? 1 : 0));
        }
        public void SetAxisView(double[] axis, bool zoomToFit)
        {
            CheckLength(axis, 3, "axis");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_axis_view(Handle(), axis, zoomToFit ? 1 : 0));
        }
        public void SetVerticalAxisView(double[] axis, bool zoomToFit)
        {
            CheckLength(axis, 3, "axis");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_vertical_axis_view(Handle(), axis, zoomToFit ? 1 : 0));
        }
        public void ZoomToFit()
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_zoom_to_fit(Handle()));
        }
        public void SetZoomFactor(double parallelScale)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_zoom_factor(Handle(), parallelScale));
        }
        public void BoxZoom(int x1, int y1, int x2, int y2)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_box_zoom(Handle(), x1, y1, x2, y2));
        }
        public void SetViewChangeDeferred(bool deferred)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_view_change_deferred(Handle(), deferred ? 1 : 0));
        }
        public double[] WorldToDisplay(double[] world)
        {
            CheckLength(world, 3, "world");
            double[] display = new double[2];
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_world_to_display(Handle(), world, display));
            return display;
        }
        public double[] DisplayToWorld(double[] display)
        {
            CheckLength(display, 2, "display");
            double[] world = new double[3];
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_display_to_world(Handle(), display, world));
            return world;
        }
        //
        // RGB rows, bottom row first
        public byte[] ReadPixels(int magnification, out int width, out int height)
        {
            IntPtr handle = Handle();
            VtkNativeStatus status = VtkNativeMethods.vtkn_view_read_pixels(handle, magnification, null, 0,
                                                                            out width, out height);
            if (status != VtkNativeStatus.BufferTooSmall) VtkNativeLibrary.Check(status);
            byte[] pixels = new byte[width * height * 3];
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_read_pixels(handle, magnification, pixels, pixels.Length,
                                                                          out width, out height));
            return pixels;
        }
        public void WritePng(string fileName, int magnification)
        {
            if (fileName == null) throw new ArgumentNullException("fileName");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_write_png(Handle(), VtkNativeLibrary.ToUtf8(fileName),
                                                                         magnification));
        }
        //
        public void Dispose()
        {
            if (_handle == IntPtr.Zero) return;
            CheckThread();
            IntPtr handle = _handle;
            _handle = IntPtr.Zero;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_destroy(handle));
            _eventCallback = null;
        }
        //
        internal IntPtr Handle()
        {
            if (_handle == IntPtr.Zero) throw new ObjectDisposedException("VtkNativeView");
            CheckThread();
            return _handle;
        }
        private void CheckThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != _threadId)
                throw new InvalidOperationException("A native view is used only on the thread that created it.");
        }
        private static IntPtr ToUnmanaged(VtkNativeMesh mesh)
        {
            IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VtkNativeMesh)));
            Marshal.StructureToPtr(mesh, pointer, false);
            return pointer;
        }
        private static void CheckLength(double[] values, int length, string argumentName)
        {
            if (values == null || values.Length != length)
                throw new ArgumentException("The argument '" + argumentName + "' must contain " + length + " values.",
                                            argumentName);
        }
        // Raised from inside VTK, so nothing may escape into native code; a handler that throws loses only its event
        private void OnNativeEvent(IntPtr userData, int eventId, IntPtr values, int valueCount)
        {
            try
            {
                EventHandler<VtkNativeEventArgs> handler = NativeEvent;
                if (handler == null) return;
                double[] copy = new double[valueCount];
                if (valueCount > 0) Marshal.Copy(values, copy, 0, valueCount);
                handler(this, new VtkNativeEventArgs((VtkNativeEvent)eventId, copy));
            }
            catch
            {
            }
        }
    }
}
