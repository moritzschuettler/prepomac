// PrePoMax for macOS - the external native view: a view without a window of its own that draws with the OpenGL context
// of the Avalonia control hosting it (see VtkNativeHost and vtkn_view_create_external in vtkNativeCore.h).

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace vtkControl.Native
{
    internal static partial class VtkNativeMethods
    {
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_create_external(out IntPtr view);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_attach_external_context(IntPtr view);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_detach_external_context(IntPtr view);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_render_external(IntPtr view, uint framebuffer, int width, int height);
    }

    public sealed partial class VtkNativeView
    {
        // Constructors
        private VtkNativeView(IntPtr handle)
        {
            _handle = handle;
            _threadId = Thread.CurrentThread.ManagedThreadId;
            _offScreen = false;
            // The delegate is kept in a field for as long as the native side holds its pointer
            _eventCallback = OnNativeEvent;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_event_callback(_handle, _eventCallback, IntPtr.Zero));
        }


        // Static methods
        public static VtkNativeView CreateExternal(int width, int height)
        {
            VtkNativeLibrary.EnsureLoaded();
            IntPtr handle;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_create_external(out handle));
            VtkNativeView view = new VtkNativeView(handle);
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


        // Methods
        // With the context of the host current, once the host created it
        public void AttachExternalContext()
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_attach_external_context(Handle()));
        }
        // With the context still alive, before the host destroys it
        public void DetachExternalContext()
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_detach_external_context(Handle()));
        }
        // In the draw callback of the host: the last frame into the bound framebuffer of the host
        public void RenderExternal(uint framebuffer, int width, int height)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_render_external(Handle(), framebuffer, width, height));
        }
    }
}
