// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.InteropServices;

namespace CaeMesh
{
    /// <summary>
    /// The calls of lp_solve 5.5 the interval assignment needs, from lpsolve55.dll, which sits next to GmshCaller.exe.
    /// The dll is built with __stdcall, which x64 ignores. Columns and rows are numbered from 1, and MYBOOL is a byte.
    /// </summary>
    internal static class LpSolve
    {
        // Variables
        private const string _dllName = "lpsolve55.dll";
        public const int LE = 1;
        public const int GE = 2;
        public const int EQ = 3;
        public const int Optimal = 0;
        public const int SubOptimal = 1;
        public const int Infeasible = 2;
        public const int Unbounded = 3;
        public const int Timeout = 7;
        public const int Critical = 1;
        public const double Infinity = 1E30;


        // Methods
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr make_lp(int rows, int columns);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void delete_lp(IntPtr lp);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall)]
        public static extern byte set_add_rowmode(IntPtr lp, byte turnOn);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall)]
        public static extern byte set_obj_fnex(IntPtr lp, int count, double[] row, int[] columnIds);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall)]
        public static extern byte add_constraintex(IntPtr lp, int count, double[] row, int[] columnIds, int constraintType,
                                                   double rightHandSide);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall)]
        public static extern byte set_bounds(IntPtr lp, int columnId, double lower, double upper);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall)]
        public static extern byte set_int(IntPtr lp, int columnId, byte mustBeInteger);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void set_minim(IntPtr lp);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void set_verbose(IntPtr lp, int verbose);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void set_timeout(IntPtr lp, int seconds);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int solve(IntPtr lp);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall)]
        public static extern double get_objective(IntPtr lp);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall)]
        public static extern byte get_variables(IntPtr lp, double[] values);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern byte write_lp(IntPtr lp, string fileName);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern byte set_col_name(IntPtr lp, int columnId, string name);
        [DllImport(_dllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern byte set_row_name(IntPtr lp, int rowId, string name);
    }
}
