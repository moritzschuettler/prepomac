// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using Python.Runtime;
using System;

namespace PrePoMax.API.Python
{
    internal static class NumPyConverter
    {
        // Methods
        public static object ToPythonArray(object values)
        {
            if (values == null) return null;
            try
            {
                using (Py.GIL())
                using (PyObject numpy = Py.Import("numpy"))
                using (PyObject array = numpy.GetAttr("array"))
                using (PyObject source = values.ToPython())
                {
                    return array.Invoke(source);
                }
            }
            catch
            {
                if (values is int[] intValues) return new PrePoMax.API.Python.IntListAPI(intValues);
                return values;
            }
        }
        public static double[,] ToRectangular(double[][] values, int columns)
        {
            int rows = values != null ? values.Length : 0;
            double[,] result = new double[rows, columns];
            for (int i = 0; i < rows; i++)
            {
                if (values[i] == null) continue;
                int count = Math.Min(columns, values[i].Length);
                for (int j = 0; j < count; j++) result[i, j] = values[i][j];
            }
            return result;
        }
    }
}
