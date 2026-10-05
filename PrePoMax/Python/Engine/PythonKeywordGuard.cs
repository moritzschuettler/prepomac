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

namespace PrePoMax.Python
{
    // pythonnet binds a keyword argument to the parameter of that name and ignores one that names no parameter,
    // so a misspelled argument, or one the method does not take, would leave its value unset without a word.
    // Every API method is therefore wrapped on its class by a guard that refuses such a call the way Python
    // refuses it for a function of its own, before the method is bound.
    //
    // A class does not let a method of its own be replaced, so the guard is written into the class dictionary
    // directly, which is what pythonnet itself does for any other attribute; setting the marker attribute
    // afterwards goes through pythonnet, which tells Python the class has changed.
    public static class PythonKeywordGuard
    {
        // Variables
        private const string GuardSource =
            "import clr, difflib, gc, importlib, inspect\n" +
            "from System.Reflection import BindingFlags\n" +
            "\n" +
            "MARKER = '__prepomax_keyword_guard__'\n" +
            "FLAGS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static\n" +
            "\n" +
            "def refusal(name, overloads, positional_count, keywords):\n" +
            "    names = [n for parameters in overloads for n in parameters]\n" +
            "    for keyword in keywords:\n" +
            "        if all(keyword in parameters[:positional_count] for parameters in overloads):\n" +
            "            return name + '() got multiple values for argument ' + repr(keyword)\n" +
            "        if keyword not in names:\n" +
            "            message = name + '() got an unexpected keyword argument ' + repr(keyword)\n" +
            "            matches = difflib.get_close_matches(keyword, names, 1)\n" +
            "            if matches:\n" +
            "                message += '. Did you mean ' + repr(matches[0]) + '?'\n" +
            "            return message\n" +
            "    return name + '() takes no combination of the keyword arguments ' + ', '.join(keywords)\n" +
            "\n" +
            "def guard(owner, name, method, overloads):\n" +
            "    def call(self, *args, **kwargs):\n" +
            "        if kwargs and not any(len(args) <= len(parameters) and\n" +
            "                              all(keyword in parameters[len(args):] for keyword in kwargs)\n" +
            "                              for parameters in overloads):\n" +
            "            raise TypeError(refusal(name, overloads, len(args), list(kwargs)))\n" +
            "        return method.__get__(self, type(self))(*args, **kwargs)\n" +
            "    call.__name__ = name\n" +
            "    call.__doc__ = method.__doc__\n" +
            "    try:\n" +
            "        signature = method.__get__(None, owner).__signature__\n" +
            "    except Exception:\n" +
            "        signature = None\n" +
            "    if signature is not None:\n" +
            "        self_parameter = inspect.Parameter('self', inspect.Parameter.POSITIONAL_ONLY)\n" +
            "        call.__signature__ = signature.replace(\n" +
            "            parameters=[self_parameter] + list(signature.parameters.values()))\n" +
            "    return call\n" +
            "\n" +
            "def guard_class(cls):\n" +
            "    if MARKER in cls.__dict__:\n" +
            "        return\n" +
            "    clr_type = clr.GetClrType(cls)\n" +
            "    members = gc.get_referents(cls.__dict__)[0]\n" +
            "    for name, method in list(members.items()):\n" +
            "        if name.startswith('_') or type(method).__name__ != 'MethodObject':\n" +
            "            continue\n" +
            "        methods = [m for m in clr_type.GetMethods(FLAGS) if m.Name == name]\n" +
            "        if not methods or any(m.IsStatic for m in methods):\n" +
            "            continue\n" +
            "        overloads = [[p.Name for p in m.GetParameters()] for m in methods]\n" +
            "        if all(len(parameters) == 0 for parameters in overloads):\n" +
            "            continue\n" +
            "        members[name] = guard(cls, name, method, overloads)\n" +
            "    setattr(cls, MARKER, True)\n" +
            "\n" +
            "def guard_assembly(assembly, base_type):\n" +
            "    for clr_type in assembly.GetExportedTypes():\n" +
            "        if clr_type.IsAbstract or clr_type.ContainsGenericParameters or clr_type.IsNested:\n" +
            "            continue\n" +
            "        if not base_type.IsAssignableFrom(clr_type):\n" +
            "            continue\n" +
            "        cls = getattr(importlib.import_module(clr_type.Namespace), clr_type.Name)\n" +
            "        for ancestor in cls.__mro__:\n" +
            "            try:\n" +
            "                ancestor_type = clr.GetClrType(ancestor)\n" +
            "            except Exception:\n" +
            "                continue\n" +
            "            if ancestor_type.Assembly.Equals(assembly):\n" +
            "                guard_class(ancestor)\n";


        // Methods
        public static void Install()
        {
            using (PyDict scope = new PyDict())
            using (PyObject assembly = typeof(BaseAPI).Assembly.ToPython())
            using (PyObject baseType = typeof(BaseAPI).ToPython())
            {
                PythonEngine.Exec(GuardSource, scope);
                using (PyObject guardAssembly = scope.GetItem("guard_assembly"))
                {
                    guardAssembly.Invoke(assembly, baseType).Dispose();
                }
            }
        }
    }
}
