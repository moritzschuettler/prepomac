// PrePoMax for macOS - the loader of the native view library: vtkControl/Native/VtkNativeLibrary.cs with the library
// loaded by the .NET native library loader instead of LoadLibraryEx of Windows.

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace vtkControl.Native
{
    // Loads libvtkNative.dylib and turns the status codes of the C API into exceptions. The library finds the VTK
    // libraries it links through its install names; the DllImport declarations of vtkNative.dll bind to it through a
    // resolver of this assembly
    public static class VtkNativeLibrary
    {
        // Variables
        private static readonly object _loadLock = new object();
        private static bool _loaded;
        private static VtkNativeMethods.LogCallback _logCallback;


        // Events
        public static event Action<int, string> LogMessage;


        // Properties
        public static bool IsLoaded { get { return _loaded; } }
        // libvtkNative.dylib next to the assemblies, in the Frameworks folder of the app bundle, or in the build folder of
        // vtkNative in the repository while developing
        public static string LibraryPath
        {
            get
            {
                string directory = Path.GetDirectoryName(typeof(VtkNativeLibrary).Assembly.Location) ?? "";
                string fileName = "libvtkNative." + (OperatingSystem.IsMacOS() ? "dylib" : "so");
                string[] candidates = { Path.Combine(directory, fileName),
                                        Path.Combine(directory, "..", "Frameworks", fileName) };
                foreach (string candidate in candidates)
                {
                    if (File.Exists(candidate)) return Path.GetFullPath(candidate);
                }
                return candidates[0];
            }
        }


        // Methods
        public static void EnsureLoaded()
        {
            if (_loaded) return;
            lock (_loadLock)
            {
                if (_loaded) return;
                //
                string path = LibraryPath;
                if (!File.Exists(path))
                    throw new VtkNativeException(VtkNativeStatus.LibraryNotLoaded, "The native library is missing: " + path);
                IntPtr module;
                try
                {
                    module = NativeLibrary.Load(path);
                }
                catch (Exception ex)
                {
                    throw new VtkNativeException(VtkNativeStatus.LibraryNotLoaded,
                                                 "The native library could not be loaded: " + path + " - " + ex.Message);
                }
                // The declarations name vtkNative.dll; they bind to the library loaded here
                NativeLibrary.SetDllImportResolver(typeof(VtkNativeLibrary).Assembly, (name, assembly, searchPath) =>
                    name == VtkNativeMethods.LibraryName ? module : IntPtr.Zero);
                //
                int apiVersion = VtkNativeMethods.vtkn_get_api_version();
                if (apiVersion != VtkNativeMethods.ApiVersion)
                    throw new VtkNativeException(VtkNativeStatus.LibraryNotLoaded,
                                                 "The native library has API version " + apiVersion + ", expected " +
                                                 VtkNativeMethods.ApiVersion + ": " + path);
                CheckAreaVersion(1, "geometry", VtkNativeMethods.GeometryApiVersion, path);
                CheckAreaVersion(2, "widgets", VtkNativeMethods.WidgetsApiVersion, path);
                CheckAreaVersion(3, "selection", VtkNativeMethods.SelectionApiVersion, path);
                //
                _logCallback = OnLog;
                Check(VtkNativeMethods.vtkn_set_log_callback(_logCallback, IntPtr.Zero));
                VtkNativeViewFont.Install();
                _loaded = true;
            }
        }
        // Each area of the API carries its own version (vtkn_area)
        private static void CheckAreaVersion(int area, string areaName, int expected, string path)
        {
            int version = VtkNativeMethods.vtkn_get_area_api_version(area);
            if (version != expected)
                throw new VtkNativeException(VtkNativeStatus.LibraryNotLoaded,
                                             "The native library has " + areaName + " API version " + version +
                                             ", expected " + expected + ": " + path);
        }
        public static string GetVtkVersion()
        {
            EnsureLoaded();
            return GetString(VtkNativeMethods.vtkn_get_vtk_version);
        }
        //
        internal static void Check(VtkNativeStatus status)
        {
            if (status == VtkNativeStatus.Ok) return;
            throw new VtkNativeException(status, status + ": " + GetLastError());
        }
        internal delegate VtkNativeStatus StringGetter(byte[] buffer, int capacity, out int required);
        // Asks for the size, then for the text, as every string out of the C API is read
        internal static string GetString(StringGetter getter)
        {
            byte[] buffer = GetBytes(getter);
            return Encoding.UTF8.GetString(buffer, 0, buffer.Length - 1);
        }
        internal static string[] GetStringList(StringGetter getter)
        {
            byte[] buffer = GetBytes(getter);
            // The list ends with an empty name, the zero byte after the last name
            string text = Encoding.UTF8.GetString(buffer, 0, buffer.Length - 1);
            if (text.Length == 0) return new string[0];
            return text.TrimEnd('\0').Split('\0');
        }
        internal static byte[] ToUtf8(string text)
        {
            if (text == null) return null;
            return Encoding.UTF8.GetBytes(text + "\0");
        }
        // A string list into the C API: every string followed by a zero byte, and one more after the last
        internal static byte[] ToUtf8List(string[] texts)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string text in texts) builder.Append(text).Append('\0');
            builder.Append('\0');
            return Encoding.UTF8.GetBytes(builder.ToString());
        }
        internal static string FromUtf8(IntPtr text)
        {
            if (text == IntPtr.Zero) return null;
            int length = 0;
            while (Marshal.ReadByte(text, length) != 0) length++;
            byte[] bytes = new byte[length];
            Marshal.Copy(text, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }
        private static byte[] GetBytes(StringGetter getter)
        {
            int required;
            VtkNativeStatus status = getter(null, 0, out required);
            if (status != VtkNativeStatus.BufferTooSmall) Check(status);
            byte[] buffer = new byte[required];
            Check(getter(buffer, buffer.Length, out required));
            return buffer;
        }
        private static string GetLastError()
        {
            int required;
            if (VtkNativeMethods.vtkn_get_last_error(null, 0, out required) != VtkNativeStatus.BufferTooSmall)
                return "The native error message could not be read.";
            byte[] buffer = new byte[required];
            if (VtkNativeMethods.vtkn_get_last_error(buffer, buffer.Length, out required) != VtkNativeStatus.Ok)
                return "The native error message could not be read.";
            return Encoding.UTF8.GetString(buffer, 0, buffer.Length - 1);
        }
        // Runs on whichever thread VTK reports from; nothing may escape into native code
        private static void OnLog(IntPtr userData, int level, IntPtr message)
        {
            try
            {
                LogMessage?.Invoke(level, FromUtf8(message));
            }
            catch
            {
            }
        }
    }
}
