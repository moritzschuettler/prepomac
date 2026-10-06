// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using Python.Runtime;

namespace APIs.Python
{
    // A short value of a fixed size - a point, a vector, a bounding box, a 3 by 3 matrix - is handed to
    // Python as a plain list, so it prints, compares and unpacks like the list a script passes in. The
    // list is built here rather than by an encoder registered with the engine, so a member returns it
    // whoever hosts the interpreter. Bulk arrays stay with NumPyConverter.
    internal static class PythonListConverter
    {
        // Methods
        public static object ToPythonList(double[] values)
        {
            using (Py.GIL())
            {
                return CreateList(values);
            }
        }
        public static object ToPythonList(double[][] rows)
        {
            using (Py.GIL())
            {
                PyList list = new PyList();
                if (rows == null) return list;
                //
                foreach (double[] row in rows)
                {
                    using (PyList item = CreateList(row)) list.Append(item);
                }
                return list;
            }
        }
        public static object ToPythonList(object[] values)
        {
            using (Py.GIL())
            {
                PyList list = new PyList();
                if (values == null) return list;
                //
                foreach (object value in values)
                {
                    using (PyObject item = value == null ? PyObject.None : value.ToPython()) list.Append(item);
                }
                return list;
            }
        }
        private static PyList CreateList(double[] values)
        {
            PyList list = new PyList();
            if (values == null) return list;
            //
            foreach (double value in values)
            {
                using (PyFloat item = new PyFloat(value)) list.Append(item);
            }
            return list;
        }
    }
}
