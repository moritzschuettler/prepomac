// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CaeGlobals
{
    /// <summary>
    /// A Windows job object that ends the processes PrePoMax starts when PrePoMax ends. A normal close kills the
    /// running jobs itself; this covers an end without that - a crash, a process killed from the Task Manager or a
    /// debugger stopped - after which a mesher or a solver kept running on its own. The processes those processes
    /// start belong to the job as well.
    /// </summary>
    public static class ChildProcessJob
    {
        // Variables
        private const int _jobObjectExtendedLimitInformation = 9;
        private const uint _jobObjectLimitKillOnJobClose = 0x2000;
        private static readonly object _lock = new object();
        private static IntPtr _job = IntPtr.Zero;
        private static bool _unavailable = false;


        // Methods
        public static void Add(Process process)
        {
            try
            {
                IntPtr job = GetJob();
                if (job != IntPtr.Zero) AssignProcessToJobObject(job, process.Handle);
            }
            catch
            { }
        }
        private static IntPtr GetJob()
        {
            lock (_lock)
            {
                if (_job != IntPtr.Zero || _unavailable) return _job;
                //
                IntPtr job = CreateJobObject(IntPtr.Zero, null);
                if (job == IntPtr.Zero)
                {
                    _unavailable = true;
                    return IntPtr.Zero;
                }
                JOBOBJECT_EXTENDED_LIMIT_INFORMATION information = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
                information.BasicLimitInformation.LimitFlags = _jobObjectLimitKillOnJobClose;
                int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
                IntPtr informationPointer = Marshal.AllocHGlobal(length);
                try
                {
                    Marshal.StructureToPtr(information, informationPointer, false);
                    if (!SetInformationJobObject(job, _jobObjectExtendedLimitInformation, informationPointer,
                                                 (uint)length))
                    {
                        CloseHandle(job);
                        _unavailable = true;
                        return IntPtr.Zero;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(informationPointer);
                }
                // The handle stays open until PrePoMax ends; closing it then is what kills the processes
                _job = job;
                return _job;
            }
        }
        //
        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObject(IntPtr jobAttributes, string name);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(IntPtr job, int infoClass, IntPtr info, uint length);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);
    }
}
