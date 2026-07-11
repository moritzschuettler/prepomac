// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using CaeResults;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PrePoMax.API.Python
{
    public static class PythonEngineManager
    {
        // Variables
        private static bool _initialized;
        private static PyDict _scope;
        private static PyObject _console;


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
            Initialize();
            //
            PrePoMaxAPI pmx = new PrePoMaxAPI(results);
            //
            using (Py.GIL())
            {
                PyObject pmxObject = pmx.ToPython();
                _scope.SetItem("pmx", pmxObject);

                pmxObject.Dispose();
            }
        }
        public static PythonConsoleResult PushLine(string line)
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
                    line = NormalizeResultPathIndexLiterals(line);
                    bool needMoreInput = console.push(line);

                    string output = stdout.getvalue().ToString();
                    string error = stderr.getvalue().ToString();

                    return new PythonConsoleResult()
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
        private static string NormalizeResultPathIndexLiterals(string line)
        {
            if (string.IsNullOrEmpty(line) || line.IndexOf('\\') < 0) return line;

            line = NormalizeResultPathIndexLiterals(line, "(?<prefix>\\bpmx\\.results\\s*\\[\\s*)'(?<value>[^']*\\\\[^']*)'");
            line = NormalizeResultPathIndexLiterals(line, "(?<prefix>\\bpmx\\.results\\s*\\[\\s*)\"(?<value>[^\"]*\\\\[^\"]*)\"");
            return line;
        }
        private static string NormalizeResultPathIndexLiterals(string line, string pattern)
        {
            return Regex.Replace(line, pattern, match =>
            {
                string value = match.Groups["value"].Value;
                if (!ContainsOddBackslashRun(value)) return match.Value;

                string prefix = match.Groups["prefix"].Value;
                char quote = match.Value[match.Value.Length - 1];
                return prefix + quote + value.Replace("\\", "\\\\") + quote;
            });
        }
        private static bool ContainsOddBackslashRun(string value)
        {
            int count = 0;
            foreach (char c in value)
            {
                if (c == '\\') count++;
                else
                {
                    if (count % 2 == 1) return true;
                    count = 0;
                }
            }
            return count % 2 == 1;
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




