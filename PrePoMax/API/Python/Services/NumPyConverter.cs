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
        // Variables
        private const string ArrayClassName = "_PrePoMaxArray";
        private const string ArrayClassCode =
            "class " + ArrayClassName + "(_np.ndarray):\n" +
            "    def __repr__(self):\n" +
            "        return _np.array2string(self, threshold=8, edgeitems=3)\n" +
            "    def __str__(self):\n" +
            "        return _np.array2string(self, threshold=self.size + 1)\n";


        // Methods
        public static object ToPythonArray(object values)
        {
            if (values == null) return null;
            try
            {
                using (Py.GIL())
                {
                    using (PyObject importlibUtil = Py.Import("importlib.util"))
                    using (PyObject findSpec = importlibUtil.GetAttr("find_spec"))
                    using (PyObject pyModuleName = new PyString("numpy"))
                    using (PyObject result = findSpec.Invoke(pyModuleName))
                    {
                        if (!result.IsNone())
                        {
                            using (PyObject numpy = Py.Import("numpy"))
                            using (PyObject array = numpy.GetAttr("array"))
                            using (PyObject source = values.ToPython())
                            using (PyObject arrayType = GetArrayType(numpy))
                            using (PyObject numpyArray = array.Invoke(source))
                            {
                                return numpyArray.InvokeMethod("view", new PyObject[] { arrayType });
                            }
                        }
                        else return ReturnArray(values);
                    }
                }
            }
            catch
            {
                return ReturnArray(values);
            }
        }
        private static object ReturnArray(object values)
        {
            if (values is int[] intValues) return new IntListAPI(intValues);
            Array array = values as Array;
            if (array != null && IsNumeric(array.GetType().GetElementType())) return new NumericArrayAPI(array);
            return values;
        }
        private static PyObject GetArrayType(PyObject numpy)
        {
            using (PyObject builtins = Py.Import("builtins"))
            {
                if (!builtins.HasAttr(ArrayClassName))
                {
                    using (PyDict scope = new PyDict())
                    {
                        scope.SetItem("_np", numpy);
                        PythonEngine.Exec(ArrayClassCode, scope, scope);
                        using (PyObject arrayType = scope.GetItem(ArrayClassName))
                        {
                            builtins.SetAttr(ArrayClassName, arrayType);
                        }
                    }
                }
                return builtins.GetAttr(ArrayClassName);
            }
        }
        private static bool IsNumeric(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }
    }
}
