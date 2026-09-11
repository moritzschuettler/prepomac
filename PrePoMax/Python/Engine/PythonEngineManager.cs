// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python;
using CaeResults;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PrePoMax.Python
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


        // Properties
        public static bool Initialized { get { return _initialized; } }


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
                CreateScopeAndConsole();
            }
            //
            _initialized = true;
        }
        private static void CreateScopeAndConsole()
        {
            _scope = new PyDict();
            dynamic code = Py.Import("code");
            _console = code.InteractiveConsole(_scope);
        }
        public static void ResetScope()
        {
            if (!_initialized) return;
            //
            using (Py.GIL())
            {
                if (_console != null) { _console.Dispose(); _console = null; }
                if (_scope != null) { _scope.Dispose(); _scope = null; }
                //
                CreateScopeAndConsole();
            }
        }
        //
        public static void SetScriptArguments(string scriptFileName, string[] arguments)
        {
            Initialize();
            //
            using (Py.GIL())
            using (PyObject sys = Py.Import("sys"))
            using (PyList argv = new PyList())
            {
                using (PyString name = new PyString(scriptFileName == null ? "" : scriptFileName))
                {
                    argv.Append(name);
                }
                if (arguments != null)
                {
                    foreach (string argument in arguments)
                    {
                        using (PyString item = new PyString(argument)) argv.Append(item);
                    }
                }
                //
                sys.SetAttr("argv", argv);
            }
        }
        public static void SetResults(ResultsCollection results)
        {
            SetResults(results, null);
        }
        public static void SetResults(ResultsCollection results, Controller controller)
        {
            Initialize();
            //
            using (Py.GIL())
            {
                SetPrePoMaxAPI(_scope, results, controller);
            }
        }
        private static void SetPrePoMaxAPI(PyDict scope, ResultsCollection results, Controller controller)
        {
            IPythonCommandGateway commandGateway = controller != null
                                                   ? new ControllerPythonCommandGateway(controller)
                                                   : null;
            IPythonSelectionGateway selectionGateway = controller != null
                                                       ? new ControllerPythonSelectionGateway(controller)
                                                       : null;
            IPythonModelGateway modelGateway = controller != null
                                               ? new ControllerPythonModelGateway(controller)
                                               : null;
            IPythonMeshGateway meshGateway = controller != null
                                             ? new ControllerPythonMeshGateway(controller)
                                             : null;
            IPythonJobGateway jobGateway = controller != null
                                           ? new ControllerPythonJobGateway(controller)
                                           : null;
            IPythonViewGateway viewGateway = controller != null
                                             ? new ControllerPythonViewGateway(controller)
                                             : null;
            PrePoMaxAPI pmx = new PrePoMaxAPI(results, commandGateway, selectionGateway, modelGateway,
                                              meshGateway, jobGateway, viewGateway);
            //
            using (PyObject pmxObject = pmx.ToPython())
            {
                scope.SetItem("pmx", pmxObject);
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

                    if (oldStdout != null) oldStdout.Dispose();
                    if (oldStderr != null) oldStderr.Dispose();
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
                    if (oldStdout != null) oldStdout.Dispose();
                    if (oldStderr != null) oldStderr.Dispose();
                }
            }
        }
        // Evaluates one expression in the shared scope - the scope the Python console uses, so an agent
        // and the user look at the same variables. The value is returned as its repr, as its text and,
        // when it has one, as JSON: a .NET collection has no JSON form of its own, so it is tried once
        // more as a list before the attempt is given up.
        public static PythonEvalResult Evaluate(string expression)
        {
            Initialize();
            //
            using (Py.GIL())
            {
                PythonEvalResult result = new PythonEvalResult();
                dynamic io = Py.Import("io");
                dynamic sys = Py.Import("sys");
                dynamic stdout = io.StringIO();
                dynamic stderr = io.StringIO();
                PyObject oldStdout = sys.stdout;
                PyObject oldStderr = sys.stderr;
                //
                try
                {
                    sys.stdout = stdout;
                    sys.stderr = stderr;
                    //
                    using (PyObject value = PythonEngine.Eval(expression, null, _scope))
                    {
                        result.Repr = GetRepr(value);
                        result.Text = value == null ? "" : value.ToString();
                        result.TypeName = GetTypeName(value);
                        result.Json = GetJson(value);
                    }
                    result.Output = stdout.getvalue().ToString();
                    result.Error = stderr.getvalue().ToString();
                }
                catch (PythonException exception)
                {
                    result.Output = stdout.getvalue().ToString();
                    result.Error = stderr.getvalue().ToString();
                    AppendError(result, FormatPythonException(exception));
                }
                catch (Exception exception)
                {
                    // A .NET exception raised by the API can reach here as itself rather than as a
                    // Python exception, and it is still what the expression did, not a protocol failure
                    result.Output = stdout.getvalue().ToString();
                    result.Error = stderr.getvalue().ToString();
                    AppendError(result, exception.Message);
                }
                finally
                {
                    sys.stdout = oldStdout;
                    sys.stderr = oldStderr;
                    if (oldStdout != null) oldStdout.Dispose();
                    if (oldStderr != null) oldStderr.Dispose();
                }
                //
                return result;
            }
        }
        // Resolves an expression to the object behind it, for the automation server describe method.
        // A value that is not an API object - a number, a string, a Python list - comes back as null,
        // because there is nothing to reflect on it.
        public static object EvaluateToApiObject(string expression, out string error)
        {
            Initialize();
            error = null;
            //
            using (Py.GIL())
            {
                try
                {
                    using (PyObject value = PythonEngine.Eval(expression, null, _scope))
                    {
                        try
                        {
                            return value.AsManagedObject(typeof(object));
                        }
                        catch (Exception)
                        {
                            return null;
                        }
                    }
                }
                catch (PythonException exception)
                {
                    error = FormatPythonException(exception);
                    return null;
                }
                catch (Exception exception)
                {
                    error = exception.Message;
                    return null;
                }
            }
        }
        private static string GetRepr(PyObject value)
        {
            if (value == null) return "None";
            using (PyObject builtins = Py.Import("builtins"))
            using (PyObject repr = builtins.InvokeMethod("repr", value))
            {
                return repr.ToString();
            }
        }
        private static string GetTypeName(PyObject value)
        {
            if (value == null) return "NoneType";
            using (PyObject type = value.GetPythonType())
            using (PyObject name = type.GetAttr("__name__"))
            {
                return name.ToString();
            }
        }
        private static string GetJson(PyObject value)
        {
            if (value == null) return "null";
            using (PyObject json = Py.Import("json"))
            {
                string text = TryDumps(json, value);
                if (text != null) return text;
                // A .NET collection reaches Python as an object rather than a list
                try
                {
                    using (PyObject builtins = Py.Import("builtins"))
                    using (PyObject list = builtins.InvokeMethod("list", value))
                    {
                        return TryDumps(json, list);
                    }
                }
                catch (PythonException)
                {
                    return null;
                }
            }
        }
        private static string TryDumps(PyObject json, PyObject value)
        {
            try
            {
                using (PyObject dumped = json.InvokeMethod("dumps", value))
                {
                    return dumped.ToString();
                }
            }
            catch (PythonException)
            {
                return null;
            }
        }
        private static void AppendError(PythonEvalResult result, string error)
        {
            if (string.IsNullOrEmpty(error)) return;
            //
            if (!string.IsNullOrEmpty(result.Error) && !result.Error.EndsWith("\n"))
                result.Error += Environment.NewLine;
            result.Error += error;
            if (!result.Error.EndsWith("\n")) result.Error += Environment.NewLine;
        }
        public static PythonEngineResult ExecuteScript(string code)
        {
            Initialize();
            //
            using (Py.GIL())
            {
                return ExecuteScript(code, _scope);
            }
        }
        public static PythonEngineResult ExecuteScriptInNewScope(string code, ResultsCollection results,
                                                                 Controller controller, string scriptFileName = null,
                                                                 string[] arguments = null)
        {
            Initialize();
            if (scriptFileName != null) SetScriptArguments(scriptFileName, arguments);
            //
            using (Py.GIL())
            using (PyDict scope = new PyDict())
            {
                using (PyObject mainName = new PyString("__main__"))
                using (PyObject fileName = new PyString(scriptFileName ?? "<string>"))
                {
                    scope.SetItem("__name__", mainName);
                    scope.SetItem("__file__", fileName);
                }
                SetPrePoMaxAPI(scope, results, controller);
                return ExecuteScript(code, scope);
            }
        }
        private static PythonEngineResult ExecuteScript(string code, PyDict scope)
        {
            PythonEngineResult result = new PythonEngineResult();
            dynamic io = Py.Import("io");
            dynamic sys = Py.Import("sys");
            dynamic stdout = io.StringIO();
            dynamic stderr = io.StringIO();
            PyObject oldStdout = sys.stdout;
            PyObject oldStderr = sys.stderr;
            //
            try
            {
                sys.stdout = stdout;
                sys.stderr = stderr;
                //
                ExecuteCompiledScriptWithTraceback(code, scope);
                //
                result.Output = stdout.getvalue().ToString();
                result.Error = stderr.getvalue().ToString();
                result.NeedMoreInput = false;
            }
            catch (PythonException ex)
            {
                result.Output = stdout.getvalue().ToString();
                result.Error = stderr.getvalue().ToString();
                string scriptError = GetScriptErrorFromScope(scope);
                //
                if (!string.IsNullOrEmpty(scriptError))
                    AppendError(result, scriptError);
                else
                    AppendError(result, FormatPythonException(ex));
                //
                result.NeedMoreInput = false;
            }
            catch (Exception ex)
            {
                result.Output = stdout.getvalue().ToString();
                result.Error = stderr.getvalue().ToString();
                string scriptError = GetScriptErrorFromScope(scope);
                //
                if (!string.IsNullOrEmpty(scriptError))
                    AppendError(result, scriptError);
                else
                    AppendError(result, ex.ToString());
                //
                result.NeedMoreInput = false;
            }
            finally
            {
                ClearScriptHelperVariables(scope);
                sys.stdout = oldStdout;
                sys.stderr = oldStderr;
                if (oldStdout != null) oldStdout.Dispose();
                if (oldStderr != null) oldStderr.Dispose();
            }
            //
            return result;
        }
        private static void ExecuteCompiledScriptWithTraceback(string code, PyDict scope)
        {
            using (PyObject builtins = Py.Import("builtins"))
            using (PyObject compileFunc = builtins.GetAttr("compile"))
            using (PyObject script = new PyString(code))
            using (PyObject fileName = scope.HasKey("__file__") ? scope.GetItem("__file__") : new PyString("<string>"))
            using (PyObject mode = new PyString("exec"))
            using (PyObject compiled = compileFunc.Invoke(script, fileName, mode))
            using (PyObject traceback = Py.Import("traceback"))
            using (PyObject emptyError = new PyString(""))
            {
                scope.SetItem(ScriptCompiledVariable, compiled);
                scope.SetItem(ScriptErrorVariable, emptyError);
                scope.SetItem(ScriptScopeVariable, scope);
                scope.SetItem(ScriptTracebackVariable, traceback);
                //
                PythonEngine.Exec(ScriptExecutionWrapper, null, scope);
            }
        }
        private static string GetScriptErrorFromScope(PyDict scope)
        {
            if (scope == null || !scope.HasKey(ScriptErrorVariable)) return "";
            //
            using (PyObject error = scope.GetItem(ScriptErrorVariable))
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
        private static void ClearScriptHelperVariables(PyDict scope)
        {
            if (scope == null) return;
            //
            foreach (string variable in ScriptHelperVariables)
            {
                if (scope.HasKey(variable))
                    scope.DelItem(variable);
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



