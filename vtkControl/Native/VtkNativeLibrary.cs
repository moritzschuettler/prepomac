// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace vtkControl.Native
{
    // Loads vtkNative.dll from the folder of this assembly and turns the status codes of the C API into exceptions.
    // The library is loaded by its full path with the altered search path, so the VTK DLLs beside it are found
    // there and the DllImport declarations bind to the module already loaded, on .NET Framework and .NET alike
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
        public static string LibraryPath
        {
            get
            {
                string directory = Path.GetDirectoryName(typeof(VtkNativeLibrary).Assembly.Location);
                return Path.Combine(directory, VtkNativeMethods.LibraryName);
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
                IntPtr module = VtkNativeMethods.LoadLibraryEx(path, IntPtr.Zero,
                                                               VtkNativeMethods.LOAD_WITH_ALTERED_SEARCH_PATH);
                if (module == IntPtr.Zero)
                {
                    string reason = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                    throw new VtkNativeException(VtkNativeStatus.LibraryNotLoaded,
                                                 "The native library could not be loaded: " + path + " - " + reason);
                }
                //
                int apiVersion = VtkNativeMethods.vtkn_get_api_version();
                if (apiVersion != VtkNativeMethods.ApiVersion)
                    throw new VtkNativeException(VtkNativeStatus.LibraryNotLoaded,
                                                 "The native library has API version " + apiVersion + ", expected " +
                                                 VtkNativeMethods.ApiVersion + ": " + path);
                //
                _logCallback = OnLog;
                Check(VtkNativeMethods.vtkn_set_log_callback(_logCallback, IntPtr.Zero));
                _loaded = true;
            }
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
