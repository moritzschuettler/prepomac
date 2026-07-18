// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PrePoMax.API.Python
{
    public static class PythonEngineManager
    {
        // Variables
        private static bool _initialized;
        private static PyDict _scope;
        private static PyObject _console;
        private const string ScriptCompiledVariable = "__prepomax_compiled_script__";
        private const string ScriptErrorVariable = "__prepomax_script_error__";
        private const string ScriptScopeVariable = "__prepomax_scope__";
        private const string ScriptTracebackVariable = "__prepomax_traceback__";
        private const string ScriptExecutionWrapper =
            "try:\n" +
            "    exec(" + ScriptCompiledVariable + ", " + ScriptScopeVariable + ", " + ScriptScopeVariable + ")\n" +
            "except BaseException:\n" +
            "    " + ScriptErrorVariable + " = " + ScriptTracebackVariable + ".format_exc()\n" +
            "    raise\n";
        private static readonly string[] ScriptHelperVariables = new string[]
        {
            ScriptCompiledVariable,
            ScriptErrorVariable,
            ScriptScopeVariable,
            ScriptTracebackVariable
        };


        // Methods
        private static void Initialize()
        {
            if (_initialized) return;
            //
            string pythonDll = Application.StartupPath + Globals.PythonDll;
            string pythonDir = Path.GetDirectoryName(pythonDll);
            //
            if (!File.Exists(pythonDll))
                throw new FileNotFoundException("Python DLL was not found.", pythonDll);
            //
            Runtime.PythonDLL = pythonDll;
            // Help Python find its standard library and extension modules.
            PythonEngine.PythonHome = pythonDir;
            PythonEngine.PythonPath = Path.Combine(pythonDir, "Lib") + Path.PathSeparator +
                                      Path.Combine(pythonDir, "DLLs") + Path.PathSeparator +
                                      Path.Combine(pythonDir, "Lib", "site-packages");
            PythonEngine.Initialize();
            // Important for WinForms/WPF apps so the UI thread is not locked forever by Python.
            PythonEngine.BeginAllowThreads();
            //
            using (Py.GIL())
            {
                _scope = new PyDict();
                dynamic code = Py.Import("code");
                _console = code.InteractiveConsole(_scope);
            }
            //
            _initialized = true;
        }
        //
        public static void SetResults(ResultsCollection results)
        {
            SetResults(results, null);
        }
        public static void SetResults(ResultsCollection results, Controller controller)
        {
            Initialize();
            //
            IPythonCommandGateway commandGateway = controller != null
                                                   ? new ControllerPythonCommandGateway(controller)
                                                   : null;
            PrePoMaxAPI pmx = new PrePoMaxAPI(results, commandGateway);
            //
            using (Py.GIL())
            {
                PyObject pmxObject = pmx.ToPython();
                _scope.SetItem("pmx", pmxObject);

                pmxObject.Dispose();
            }
        }
        public static PythonEngineResult PushLine(string line)
        {
            Initialize();

            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");
                dynamic io = Py.Import("io");

                dynamic stdout = io.StringIO();
                dynamic stderr = io.StringIO();

                PyObject oldStdout = sys.stdout;
                PyObject oldStderr = sys.stderr;

                try
                {
                    sys.stdout = stdout;
                    sys.stderr = stderr;

                    dynamic console = _console;
                    bool needMoreInput = console.push(line);

                    string output = stdout.getvalue().ToString();
                    string error = stderr.getvalue().ToString();

                    return new PythonEngineResult()
                    {
                        Output = output,
                        Error = error,
                        NeedMoreInput = needMoreInput
                    };
                }
                finally
                {
                    sys.stdout = oldStdout;
                    sys.stderr = oldStderr;

                    oldStdout.Dispose();
                    oldStderr.Dispose();
                }
            }
        }
        public static string Execute(string code)
        {
            Initialize();
            //
            using (Py.GIL())
            {
                dynamic io = Py.Import("io");
                dynamic sys = Py.Import("sys");
                dynamic stdout = io.StringIO();
                dynamic stderr = io.StringIO();
                PyObject oldStdout = sys.stdout;
                PyObject oldStderr = sys.stderr;
                try
                {
                    sys.stdout = stdout;
                    sys.stderr = stderr;
                    //
                    PythonEngine.Exec(code, null, _scope);
                    //
                    string printedOutput = stdout.getvalue().ToString();
                    string errorOutput = stderr.getvalue().ToString();
                    //
                    return printedOutput + Environment.NewLine + errorOutput;
                }
                finally
                {
                    sys.stdout = oldStdout;
                    sys.stderr = oldStderr;
                    oldStdout.Dispose();
                    oldStderr.Dispose();
                }
            }
        }
        public static PythonEngineResult ExecuteScript(string code)
        {
            Initialize();

            PythonEngineResult result = new PythonEngineResult();

            using (Py.GIL())
            {
                dynamic io = Py.Import("io");
                dynamic sys = Py.Import("sys");
                dynamic stdout = io.StringIO();
                dynamic stderr = io.StringIO();
                PyObject oldStdout = sys.stdout;
                PyObject oldStderr = sys.stderr;

                try
                {
                    sys.stdout = stdout;
                    sys.stderr = stderr;
                    //
                    ExecuteCompiledScriptWithTraceback(code);
                    //
                    result.Output = stdout.getvalue().ToString();
                    result.Error = stderr.getvalue().ToString();
                    result.NeedMoreInput = false;
                }
                catch (PythonException ex)
                {
                    result.Output = stdout.getvalue().ToString();
                    result.Error = stderr.getvalue().ToString();
                    string scriptError = GetScriptErrorFromScope();

                    if (!string.IsNullOrEmpty(scriptError))
                        AppendError(result, scriptError);
                    else
                        AppendError(result, FormatPythonException(ex));

                    result.NeedMoreInput = false;
                }
                catch (Exception ex)
                {
                    result.Output = stdout.getvalue().ToString();
                    result.Error = stderr.getvalue().ToString();
                    string scriptError = GetScriptErrorFromScope();

                    if (!string.IsNullOrEmpty(scriptError))
                        AppendError(result, scriptError);
                    else
                        AppendError(result, ex.ToString());

                    result.NeedMoreInput = false;
                }
                finally
                {
                    ClearScriptHelperVariables();
                    sys.stdout = oldStdout;
                    sys.stderr = oldStderr;
                    oldStdout.Dispose();
                    oldStderr.Dispose();
                }
            }

            return result;
        }
        private static void ExecuteCompiledScriptWithTraceback(string code)
        {
            using (PyObject builtins = Py.Import("builtins"))
            using (PyObject compileFunc = builtins.GetAttr("compile"))
            using (PyObject script = new PyString(code))
            using (PyObject fileName = new PyString("<string>"))
            using (PyObject mode = new PyString("exec"))
            using (PyObject compiled = compileFunc.Invoke(script, fileName, mode))
            using (PyObject traceback = Py.Import("traceback"))
            using (PyObject emptyError = new PyString(""))
            {
                _scope.SetItem(ScriptCompiledVariable, compiled);
                _scope.SetItem(ScriptErrorVariable, emptyError);
                _scope.SetItem(ScriptScopeVariable, _scope);
                _scope.SetItem(ScriptTracebackVariable, traceback);

                PythonEngine.Exec(ScriptExecutionWrapper, null, _scope);
            }
        }
        private static string GetScriptErrorFromScope()
        {
            if (_scope == null || !_scope.HasKey(ScriptErrorVariable)) return "";

            using (PyObject error = _scope.GetItem(ScriptErrorVariable))
            {
                return error.ToString();
            }
        }
        private static string FormatPythonException(PythonException ex)
        {
            try
            {
                ex.Normalize();

                using (PyObject traceback = Py.Import("traceback"))
                using (PyObject formatException = traceback.GetAttr("format_exception"))
                using (PyObject formattedLines = formatException.Invoke(new PyObject[]
                       {
                           ex.Type,
                           ex.Value,
                           ex.Traceback ?? PyObject.None
                       }))
                using (PyObject empty = new PyString(""))
                using (PyObject formatted = empty.InvokeMethod("join", formattedLines))
                {
                    return formatted.ToString();
                }
            }
            catch
            {
                return ex.Message + Environment.NewLine;
            }
        }
        private static void AppendError(PythonEngineResult result, string error)
        {
            if (string.IsNullOrEmpty(error)) return;

            if (!string.IsNullOrEmpty(result.Error) && !result.Error.EndsWith("\n"))
                result.Error += Environment.NewLine;

            result.Error += error;

            if (!result.Error.EndsWith("\n"))
                result.Error += Environment.NewLine;
        }
        private static void ClearScriptHelperVariables()
        {
            if (_scope == null) return;

            foreach (string variable in ScriptHelperVariables)
            {
                if (_scope.HasKey(variable))
                    _scope.DelItem(variable);
            }
        }
        // Autocompletion method for the Python console
        public static List<string> GetCompletions(string expression, string prefix)
        {
            Initialize();

            List<string> completions = new List<string>();

            using (Py.GIL())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(expression))
                        GetGlobalCompletions(prefix, completions);
                    else
                        GetObjectCompletions(expression, prefix, completions);
                }
                catch
                {
                    completions.Clear();
                }
            }

            completions.Sort(StringComparer.OrdinalIgnoreCase);
            return completions;
        }
        private static void GetGlobalCompletions(string prefix, List<string> completions)
        {
            HashSet<string> names = new HashSet<string>();

            using (PyObject keys = _scope.InvokeMethod("keys"))
            {
                AddNamesFromIterable(keys, prefix, names);
            }

            using (PyObject builtinNames = PythonEngine.Eval("dir(__import__('builtins'))", null, _scope))
            {
                AddNamesFromIterable(builtinNames, prefix, names);
            }

            completions.AddRange(names);
        }
        private static void GetObjectCompletions(string expression, string prefix, List<string> completions)
        {
            HashSet<string> names = new HashSet<string>();

            using (PyObject objectNames = PythonEngine.Eval("dir(" + expression + ")", null, _scope))
            {
                AddNamesFromIterable(objectNames, prefix, names);
            }

            completions.AddRange(names);
        }
        private static void AddNamesFromIterable(PyObject iterable, string prefix, HashSet<string> names)
        {
            using (PyObject builtins = Py.Import("builtins"))
            using (PyObject listFunc = builtins.GetAttr("list"))
            using (PyObject list = listFunc.Invoke(iterable))
            {
                long count = list.Length();

                for (int i = 0; i < count; i++)
                {
                    using (PyObject item = list[i])
                    {
                        string name = item.ToString();

                        if (IsValidCompletionName(name, prefix))
                            names.Add(name);
                    }
                }
            }
        }
        private static bool IsValidCompletionName(string name, string prefix)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (name == "help_text") return false;
            if (name.StartsWith("__", StringComparison.Ordinal)) return false;

            // Only expose names starting with lowercase letter or underscore.
            if (!(char.IsLower(name[0]) || name[0] == '_')) return false;

            if (prefix == null) prefix = "";

            return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}




