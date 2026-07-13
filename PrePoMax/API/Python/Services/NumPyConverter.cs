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
                if (IsPythonModuleAvailable("numpy"))
                {
                    using (Py.GIL())
                    using (PyObject numpy = Py.Import("numpy"))
                    using (PyObject array = numpy.GetAttr("array"))
                    using (PyObject source = values.ToPython())
                    {
                        return array.Invoke(source);
                    }
                }
                else return ReturnArray(values);
            }
            catch
            {
                return ReturnArray(values);
            }
        }
        private static bool IsPythonModuleAvailable(string moduleName)
        {
            if (!PythonEngine.IsInitialized)
                throw new InvalidOperationException("The Python engine is not initialized.");
            //
            using (Py.GIL())
            {
                try
                {
                    using (PyObject importlibUtil = Py.Import("importlib.util"))
                    using (PyObject findSpec = importlibUtil.GetAttr("find_spec"))
                    using (PyObject pyModuleName = new PyString(moduleName))
                    using (PyObject result = findSpec.Invoke(pyModuleName))
                    {
                        return !result.IsNone();
                    }
                }
                catch (PythonException)
                {
                    return false;
                }
            }
        }
        private static object ReturnArray(object values)
        {
            if (values is int[] intValues) return new IntListAPI(intValues);
            return values;
        }
    }
}
