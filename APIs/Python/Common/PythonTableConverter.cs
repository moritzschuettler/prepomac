// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python
{
    // Turns an argument given as a list of rows into a table of a fixed width. The material properties
    // keep their own converter, because their last column is always the temperature and their single
    // row form takes the values as separate arguments.
    internal static class PythonTableConverter
    {
        // Methods
        public static double[][] ToTable(object value, string argumentName, int columnCount)
        {
            if (value == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            //
            double[][] rows = PythonDoubleListConverter.ToDoubleArray2D(value, argumentName);
            if (rows == null || rows.Length == 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must contain at least one row.");
            //
            foreach (double[] row in rows)
            {
                if (row == null || row.Length != columnCount)
                    throw new PythonAPIException("Every row of the argument '" + argumentName + "' must contain " +
                                                 columnCount + " values.");
            }
            //
            return rows;
        }
    }
}
