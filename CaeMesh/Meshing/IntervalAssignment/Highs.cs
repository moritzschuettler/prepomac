// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CaeMesh
{
    /// <summary>
    /// The calls of the C interface of HiGHS the interval assignment and the sweep of a compound need, from highs.dll,
    /// which sits next to GmshCaller.exe and CaeMesh.dll. Columns and rows are numbered from 0, and HighsInt is a
    /// 32-bit integer in this build. The solver runs on one thread and returns the same solution on every run.
    /// </summary>
    internal static class Highs
    {
        // Variables
        private const string _dllName = "highs.dll";
        public const int Error = -1;
        public const int Continuous = 0;
        public const int Integer = 1;
        public const int Minimize = 1;
        public const int Maximize = -1;
        public const int Optimal = 7;
        public const int Infeasible = 8;
        public const int UnboundedOrInfeasible = 9;
        public const int Unbounded = 10;
        public const int TimeLimit = 13;
        public const int FeasibleSolution = 2;
        public const double Infinity = double.PositiveInfinity;


        // Constructors
        /// <summary>
        /// The dll is loaded from the folder of this assembly before the first call. GmshCaller.exe finds it there by
        /// itself, but PrePoMax.exe, which runs from the folder above, would not.
        /// </summary>
        static Highs()
        {
#if NET
            // macOS: libhighs.dylib next to the program (app bundle) or from Homebrew (brew install highs)
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                try
                {
                    NativeLibrary.SetDllImportResolver(typeof(Highs).Assembly, (name, assembly, searchPath) =>
                    {
                        if (name != _dllName) return IntPtr.Zero;
                        string folder = Path.GetDirectoryName(typeof(Highs).Assembly.Location) ?? "";
                        foreach (string candidate in new[] { Path.Combine(folder, "libhighs.dylib"),
                                                             Path.Combine(folder, "..", "Frameworks", "libhighs.dylib"),
                                                             "/opt/homebrew/lib/libhighs.dylib",
                                                             "/usr/local/lib/libhighs.dylib",
                                                             // Linux (experimental)
                                                             "/usr/lib/libhighs.so",
                                                             "/usr/local/lib/libhighs.so" })
                        {
                            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out IntPtr handle)) return handle;
                        }
                        return IntPtr.Zero;
                    });
                }
                catch (InvalidOperationException)
                {
                    // A resolver of the assembly is set already
                }
                return;
            }
#endif
            try
            {
                string folder = Path.GetDirectoryName(typeof(Highs).Assembly.Location);
                string fileName = folder != null ? Path.Combine(folder, _dllName) : null;
                if (fileName != null && File.Exists(fileName)) LoadLibrary(fileName);
            }
            catch (Exception)
            {
                // A dll that cannot be loaded here is looked for the usual way by the first call
            }
        }


        // Methods
        /// <summary>
        /// A model without output that is solved on one thread, to the best solution there is rather than to within
        /// the relative gap HiGHS stops at by default, in at most the given time.
        /// </summary>
        public static IntPtr Create(int timeoutSeconds)
        {
            IntPtr highs;
            try
            {
                highs = Highs_create();
            }
            catch (Exception ex) when (ex is DllNotFoundException || ex is BadImageFormatException ||
                                       ex is EntryPointNotFoundException)
            {
                throw new CaeException("HiGHS, which settles the numbers of elements on the edges and the ways of " +
                                       "sweeping, could not be loaded from " + _dllName + " next to GmshCaller.exe: " +
                                       ex.Message);
            }
            if (highs == IntPtr.Zero) throw new CaeException("HiGHS could not create a model.");
            try
            {
                Check(Highs_setBoolOptionValue(highs, "output_flag", 0));
                Check(Highs_setIntOptionValue(highs, "threads", 1));
                Check(Highs_setDoubleOptionValue(highs, "mip_rel_gap", 0));
                Check(Highs_setDoubleOptionValue(highs, "time_limit", timeoutSeconds));
            }
            catch (Exception)
            {
                Highs_destroy(highs);
                throw;
            }
            return highs;
        }
        public static void Check(int result)
        {
            if (result == Error) throw new CaeException("HiGHS refused a part of the model it was given.");
        }
        /// <summary>
        /// Whether the last run left a solution that satisfies the model, which a run stopped by its time limit may.
        /// </summary>
        public static bool HasFeasibleSolution(IntPtr highs)
        {
            int solutionStatus;
            return Highs_getIntInfoValue(highs, "primal_solution_status", out solutionStatus) != Error &&
                   solutionStatus == FeasibleSolution;
        }
        public static double[] GetColumnValues(IntPtr highs, int numOfColumns)
        {
            int numOfRows = Highs_getNumRow(highs);
            double[] columnValues = new double[numOfColumns];
            Check(Highs_getSolution(highs, columnValues, new double[numOfColumns], new double[numOfRows],
                                    new double[numOfRows]));
            return columnValues;
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string fileName);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Highs_create();
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Highs_destroy(IntPtr highs);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int Highs_setBoolOptionValue(IntPtr highs, string option, int value);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int Highs_setIntOptionValue(IntPtr highs, string option, int value);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int Highs_setDoubleOptionValue(IntPtr highs, string option, double value);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Highs_addVars(IntPtr highs, int numOfColumns, double[] lower, double[] upper);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Highs_addRow(IntPtr highs, double lower, double upper, int count, int[] columnIds,
                                              double[] values);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Highs_changeColsCostByRange(IntPtr highs, int fromColumnId, int toColumnId,
                                                             double[] costs);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Highs_changeColsIntegralityByRange(IntPtr highs, int fromColumnId, int toColumnId,
                                                                    int[] integrality);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Highs_changeObjectiveSense(IntPtr highs, int sense);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Highs_zeroAllClocks(IntPtr highs);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Highs_run(IntPtr highs);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Highs_getModelStatus(IntPtr highs);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Highs_getObjectiveValue(IntPtr highs);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int Highs_getNumRow(IntPtr highs);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int Highs_getSolution(IntPtr highs, double[] columnValues, double[] columnDuals,
                                                    double[] rowValues, double[] rowDuals);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int Highs_getIntInfoValue(IntPtr highs, string info, out int value);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int Highs_writeModel(IntPtr highs, string fileName);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int Highs_passColName(IntPtr highs, int columnId, string name);
        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int Highs_passRowName(IntPtr highs, int rowId, string name);
    }
}
