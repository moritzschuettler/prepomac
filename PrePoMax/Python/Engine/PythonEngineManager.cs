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
using PrePoMax.Rpc;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace PrePoMax.Python
{
    public static class PythonEngineManager
    {
        // Variables
        private static readonly object _initializationLock = new object();
        private static volatile bool _initialized;
        private static bool _engineStarted;
        private static bool _dictionaryEncoderRegistered;
        private static bool _apiExceptionEncoderRegistered;
        private static bool _keywordGuardInstalled;
        private static Exception _initializationError;
        private static PyDict _scope;
        private static PyObject _console;
        private static PyObject _capturedOutputClass;
        private static readonly object _executionLock = new object();
        private static readonly Dictionary<int, PythonExecution> _executions = new Dictionary<int, PythonExecution>();
        // What a run keeps of what it prints: the beginning, where the first error usually is, and the end, where
        // the last one is. Anything between them is counted and left out, so a print in a runaway loop can not
        // exhaust the memory of the application or bury an agent's answer
        public const int CapturedOutputHeadLength = 1000000;
        public const int CapturedOutputTailLength = 100000;
        private const string CapturedOutputSource =
            "import io\n" +
            "\n" +
            "class CapturedOutput(io.TextIOBase):\n" +
            "    def __init__(self, head_limit, tail_limit):\n" +
            "        super().__init__()\n" +
            "        self._head_limit = head_limit\n" +
            "        self._tail_limit = tail_limit\n" +
            "        self._head = []\n" +
            "        self._head_length = 0\n" +
            "        self._tail = []\n" +
            "        self._tail_length = 0\n" +
            "        self._omitted = 0\n" +
            "\n" +
            "    def writable(self):\n" +
            "        return True\n" +
            "\n" +
            "    def write(self, text):\n" +
            "        if not isinstance(text, str):\n" +
            "            raise TypeError('write() argument must be str, not ' + type(text).__name__)\n" +
            "        length = len(text)\n" +
            "        room = self._head_limit - self._head_length\n" +
            "        if room > 0:\n" +
            "            part = text[:room]\n" +
            "            self._head.append(part)\n" +
            "            self._head_length += len(part)\n" +
            "            text = text[room:]\n" +
            "        if text:\n" +
            "            self._tail.append(text)\n" +
            "            self._tail_length += len(text)\n" +
            "            if self._tail_length > 2 * self._tail_limit:\n" +
            "                self._trim()\n" +
            "        return length\n" +
            "\n" +
            "    def _trim(self):\n" +
            "        tail = ''.join(self._tail)\n" +
            "        cut = len(tail) - self._tail_limit\n" +
            "        if cut > 0:\n" +
            "            self._omitted += cut\n" +
            "            tail = tail[cut:]\n" +
            "        self._tail = [tail]\n" +
            "        self._tail_length = len(tail)\n" +
            "\n" +
            "    def getvalue(self):\n" +
            "        self._trim()\n" +
            "        head = ''.join(self._head)\n" +
            "        if self._omitted == 0:\n" +
            "            return head + self._tail[0]\n" +
            "        marker = '[output truncated: ' + str(self._omitted) + ' characters omitted]\\n'\n" +
            "        if head and not head.endswith('\\n'):\n" +
            "            marker = '\\n' + marker\n" +
            "        return head + marker + self._tail[0]\n";
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
        // Python someone wrote is running on some thread - an automation call, a script of the Python panel
        public static bool IsExecuting { get { lock (_executionLock) return _executions.Count > 0; } }


        // Methods
        // The engine is started by whichever thread asks first - the user interface thread when the Python
        // panel opens, an automation call thread otherwise - so the start is locked. It is never retried
        // half way: BeginAllowThreads on a thread that does not hold the GIL is fatal, and a second
        // exception encoder would replace the PythonAPIException class scripts catch. A start that failed
        // reports its first error on every later attempt instead
        private static void Initialize()
        {
            if (_initialized) return;
            //
            lock (_initializationLock)
            {
                if (_initialized) return;
                if (_initializationError != null)
                {
                    throw new InvalidOperationException("The Python engine failed to start earlier in this session " +
                                                        "and is not started again: " + _initializationError.Message +
                                                        " Restart PrePoMax to try again.", _initializationError);
                }
                //
                try
                {
                    StartEngine();
                    using (Py.GIL())
                    {
                        RegisterEncoders();
                        if (!_keywordGuardInstalled)
                        {
                            PythonKeywordGuard.Install();
                            _keywordGuardInstalled = true;
                        }
                        if (_capturedOutputClass == null) _capturedOutputClass = CreateCapturedOutputClass();
                        CreateScopeAndConsole();
                    }
                    //
                    _initialized = true;
                }
                catch (Exception exception)
                {
                    _initializationError = exception;
                    throw;
                }
            }
        }
        private static void StartEngine()
        {
            if (_engineStarted) return;
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
            _engineStarted = true;
        }
        private static void RegisterEncoders()
        {
            if (!_dictionaryEncoderRegistered)
            {
                PyObjectConversions.RegisterEncoder(new PythonDictionaryEncoder());
                _dictionaryEncoderRegistered = true;
            }
            if (!_apiExceptionEncoderRegistered)
            {
                PyObjectConversions.RegisterEncoder(new PythonApiExceptionEncoder());
                _apiExceptionEncoderRegistered = true;
            }
        }
        private static PyObject CreateCapturedOutputClass()
        {
            using (PyDict scope = new PyDict())
            {
                PythonEngine.Exec(CapturedOutputSource, scope);
                return scope.GetItem("CapturedOutput");
            }
        }
        // Takes the place of sys.stdout or sys.stderr for one run. Called with the GIL held
        private static PyObject CreateCapturedOutput()
        {
            using (PyObject headLength = CapturedOutputHeadLength.ToPython())
            using (PyObject tailLength = CapturedOutputTailLength.ToPython())
            {
                return _capturedOutputClass.Invoke(headLength, tailLength);
            }
        }
        // Marks the calling thread as running Python someone wrote, so that it can be interrupted. Called with the
        // GIL held; a run inside a run - a recorded script regenerated by a script - belongs to the outer one.
        // An automation call cancelled before its Python started - while it waited for the GIL, say - is
        // interrupted here, so the cancel is raised at its first statement rather than lost
        private static PythonExecution BeginExecution()
        {
            int managedThreadId = Thread.CurrentThread.ManagedThreadId;
            lock (_executionLock)
            {
                if (_executions.ContainsKey(managedThreadId)) return null;
            }
            PythonExecution execution = new PythonExecution(managedThreadId, PythonEngine.GetPythonThreadID());
            lock (_executionLock) _executions.Add(managedThreadId, execution);
            //
            RpcRunningCall call = RpcRunningCall.Current;
            if (call != null && call.AttachExecution(execution))
            {
                lock (_executionLock)
                {
                    if (PythonEngine.Interrupt(execution.PythonThreadId) > 0) execution.DeliveredInterruptCount++;
                }
            }
            return execution;
        }
        // Called with the GIL held, which the interrupt is delivered under as well, so no interrupt can reach the
        // thread after this. One that was delivered after the last statement it would have landed in is still
        // pending on the thread, and is discarded here rather than left to stop the next run
        private static void EndExecution(PythonExecution execution)
        {
            if (execution == null) return;
            //
            RpcRunningCall call = RpcRunningCall.Current;
            if (call != null) call.DetachExecution(execution);
            //
            bool isInterruptDelivered;
            lock (_executionLock)
            {
                _executions.Remove(execution.ManagedThreadId);
                execution.IsEnded = true;
                isInterruptDelivered = execution.IsInterruptDelivered;
                Monitor.PulseAll(_executionLock);
            }
            if (isInterruptDelivered)
            {
                try
                {
                    PythonEngine.Exec("pass");
                }
                catch (PythonException)
                {
                }
            }
        }
        // The run the given thread is in, or null when it is not running Python
        public static PythonExecution GetExecution(int managedThreadId)
        {
            PythonExecution execution;
            lock (_executionLock)
            {
                if (_executions.TryGetValue(managedThreadId, out execution)) return execution;
            }
            return null;
        }
        // Raises KeyboardInterrupt in the given run, which stops it at the next statement it executes. Python
        // inside an operation of the application - meshing, an import - holds the GIL the interrupt needs and is
        // not interrupted until that operation returns to Python; a blocking wait such as time.sleep, a socket or
        // input() is not interrupted either, and the interrupt is raised once it returns. Every request delivers
        // an interrupt of its own, because the run may have caught the one before, but a request made while one
        // still waits for the GIL joins it rather than adding another. The interrupt is delivered from a thread of
        // its own and the caller waits up to the given time to learn whether it was; it must not hold the GIL.
        // Returns false when the run had ended, or ended before the interrupt reached it
        public static bool RequestInterrupt(PythonExecution execution, int waitMilliseconds, out bool isDelivered)
        {
            isDelivered = false;
            if (execution == null) return false;
            //
            int deliveredBefore;
            bool isDeliveryStarted = false;
            lock (_executionLock)
            {
                if (execution.IsEnded) return false;
                deliveredBefore = execution.DeliveredInterruptCount;
                if (!execution.IsDeliveryPending)
                {
                    execution.IsDeliveryPending = true;
                    isDeliveryStarted = true;
                }
            }
            if (isDeliveryStarted)
            {
                Thread thread = new Thread(delegate () { DeliverInterrupt(execution); });
                thread.IsBackground = true;
                thread.Name = "PrePoMax Python interrupt";
                thread.Start();
            }
            //
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(0, waitMilliseconds));
            lock (_executionLock)
            {
                while (execution.DeliveredInterruptCount == deliveredBefore && execution.IsDeliveryPending &&
                       !execution.IsEnded)
                {
                    int left = (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalMilliseconds);
                    if (left <= 0) break;
                    Monitor.Wait(_executionLock, left);
                }
                isDelivered = execution.DeliveredInterruptCount > deliveredBefore;
                return isDelivered || !execution.IsEnded;
            }
        }
        private static void DeliverInterrupt(PythonExecution execution)
        {
            try
            {
                using (Py.GIL())
                {
                    lock (_executionLock)
                    {
                        PythonExecution current;
                        if (!_executions.TryGetValue(execution.ManagedThreadId, out current) || current != execution)
                            return;
                        if (PythonEngine.Interrupt(execution.PythonThreadId) > 0) execution.DeliveredInterruptCount++;
                    }
                }
            }
            catch (Exception)
            {
                // An exception on this thread would end the application, and a failed interrupt only leaves the
                // run going as it would have without one
            }
            finally
            {
                lock (_executionLock)
                {
                    execution.IsDeliveryPending = false;
                    Monitor.PulseAll(_executionLock);
                }
            }
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

                dynamic stdout = CreateCapturedOutput();
                dynamic stderr = CreateCapturedOutput();

                PyObject oldStdout = sys.stdout;
                PyObject oldStderr = sys.stderr;

                try
                {
                    sys.stdout = stdout;
                    sys.stderr = stderr;

                    dynamic console = _console;
                    bool needMoreInput;
                    PythonException consoleException = null;
                    PythonExecution execution = BeginExecution();
                    try
                    {
                        needMoreInput = console.push(line);
                    }
                    catch (PythonException exception)
                    {
                        // The console reports what the code raised itself, so this is an interrupt that landed in
                        // the console between two of its own statements, and the line it was collecting is dropped
                        consoleException = exception;
                        needMoreInput = false;
                    }
                    finally
                    {
                        EndExecution(execution);
                    }
                    if (consoleException != null) console.resetbuffer();

                    string output = stdout.getvalue().ToString();
                    string error = stderr.getvalue().ToString();
                    if (consoleException != null) error += FormatPythonException(consoleException);

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
                dynamic sys = Py.Import("sys");
                dynamic stdout = CreateCapturedOutput();
                dynamic stderr = CreateCapturedOutput();
                PyObject oldStdout = sys.stdout;
                PyObject oldStderr = sys.stderr;
                try
                {
                    sys.stdout = stdout;
                    sys.stderr = stderr;
                    //
                    PythonExecution execution = BeginExecution();
                    try
                    {
                        PythonEngine.Exec(code, null, _scope);
                    }
                    finally
                    {
                        EndExecution(execution);
                    }
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
                dynamic sys = Py.Import("sys");
                dynamic stdout = CreateCapturedOutput();
                dynamic stderr = CreateCapturedOutput();
                PyObject oldStdout = sys.stdout;
                PyObject oldStderr = sys.stderr;
                //
                try
                {
                    sys.stdout = stdout;
                    sys.stderr = stderr;
                    //
                    PythonExecution execution = BeginExecution();
                    try
                    {
                        using (PyObject value = PythonEngine.Eval(expression, null, _scope))
                        {
                            result.Repr = GetRepr(value);
                            result.Text = value == null ? "" : value.ToString();
                            result.TypeName = GetTypeName(value);
                            result.Json = GetJson(value);
                        }
                    }
                    finally
                    {
                        EndExecution(execution);
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
        // NaN and infinity have no JSON form, and the default json.dumps writes them as bare tokens
        // that break the parse of the whole answer, so they are written as null; a key can not be null, so such
        // a key is written as its text
        private const string FiniteJsonSource =
            "import json, math\n" +
            "def _finite_key(key):\n" +
            "    if isinstance(key, float) and not math.isfinite(key):\n" +
            "        return str(key)\n" +
            "    return key\n" +
            "def _finite(value):\n" +
            "    if isinstance(value, float):\n" +
            "        return value if math.isfinite(value) else None\n" +
            "    if isinstance(value, dict):\n" +
            "        return {_finite_key(key): _finite(item) for key, item in value.items()}\n" +
            "    if isinstance(value, (list, tuple)):\n" +
            "        return [_finite(item) for item in value]\n" +
            "    return value\n" +
            "def dumps(value):\n" +
            "    return json.dumps(_finite(value), allow_nan=False)\n";
        private static string GetJson(PyObject value)
        {
            if (value == null) return "null";
            using (PyObject builtins = Py.Import("builtins"))
            using (PyDict scope = new PyDict())
            {
                // The scope is the globals of the functions it defines, which find json and _finite there
                scope.SetItem("__builtins__", builtins);
                PythonEngine.Exec(FiniteJsonSource, scope);
                using (PyObject dumps = scope.GetItem("dumps"))
                {
                    string text = TryDumps(dumps, value);
                    if (text != null) return text;
                    // A .NET collection reaches Python as an object rather than a list
                    try
                    {
                        using (PyObject list = builtins.InvokeMethod("list", value))
                        {
                            return TryDumps(dumps, list);
                        }
                    }
                    catch (PythonException exception)
                    {
                        if (IsKeyboardInterrupt(exception)) throw;
                        return null;
                    }
                }
            }
        }
        // A value without a JSON form is no error of the expression, but an interrupt landing while it is tried
        // is a Stop or a cancel and must not be swallowed with it
        private static string TryDumps(PyObject dumps, PyObject value)
        {
            try
            {
                using (PyObject dumped = dumps.Invoke(value))
                {
                    return dumped.ToString();
                }
            }
            catch (PythonException exception)
            {
                if (IsKeyboardInterrupt(exception)) throw;
                return null;
            }
        }
        private static bool IsKeyboardInterrupt(PythonException exception)
        {
            try
            {
                using (PyObject name = exception.Type.GetAttr("__name__"))
                {
                    return name.ToString() == "KeyboardInterrupt";
                }
            }
            catch (Exception)
            {
                return false;
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
            dynamic sys = Py.Import("sys");
            dynamic stdout = CreateCapturedOutput();
            dynamic stderr = CreateCapturedOutput();
            PyObject oldStdout = sys.stdout;
            PyObject oldStderr = sys.stderr;
            //
            try
            {
                sys.stdout = stdout;
                sys.stderr = stderr;
                //
                PythonExecution execution = BeginExecution();
                try
                {
                    ExecuteCompiledScriptWithTraceback(code, scope);
                }
                finally
                {
                    EndExecution(execution);
                }
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



