// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python;
using Python.Runtime;
using System;

namespace PrePoMax.Python
{
    // pythonnet raises a .NET exception in Python as a wrapper whose text is the exception's ToString, so a
    // refusal the API wrote for the user reached them as "APIs.Python.PythonAPIException: ..." followed by the
    // .NET stack trace of the application. A refusal is not a fault of the application, so it is raised as a
    // plain Python exception of the same name instead, carrying only its message. The class is a builtin, so a
    // script catches it as PythonAPIException without importing anything, and it derives from Exception.
    // Every other .NET exception is a bug and keeps its wrapper and its stack trace.
    public class PythonApiExceptionEncoder : IPyObjectEncoder
    {
        // Variables
        private const string ExceptionClassSource =
            "import builtins\n" +
            "\n" +
            "class PythonAPIException(Exception):\n" +
            "    pass\n" +
            "\n" +
            "PythonAPIException.__module__ = 'builtins'\n" +
            "builtins.PythonAPIException = PythonAPIException\n";
        private readonly PyObject _exceptionClass;


        // Constructors
        public PythonApiExceptionEncoder()
        {
            using (PyDict scope = new PyDict())
            {
                PythonEngine.Exec(ExceptionClassSource, scope);
                _exceptionClass = scope.GetItem("PythonAPIException");
            }
        }


        // Methods
        public bool CanEncode(Type type)
        {
            // pythonnet asks with the declared type of the raised exception, which is Exception
            return typeof(Exception).IsAssignableFrom(type) && type.IsAssignableFrom(typeof(PythonAPIException));
        }
        public PyObject TryEncode(object value)
        {
            PythonAPIException exception = value as PythonAPIException;
            if (exception == null) return null;
            //
            using (PyObject message = new PyString(exception.Message ?? ""))
            {
                return _exceptionClass.Invoke(message);
            }
        }
    }
}
