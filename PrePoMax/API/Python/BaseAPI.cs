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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PrePoMax.API.Python
{
    public class BaseAPI
    {
        // Methods
        public virtual string __str__()
        {
            return ToFullString();
        }
        public virtual string __repr__()
        {
            return ToString();
        }
        public string[] __dir__()
        {
            return GetPythonMemberNames().ToArray();
        }

        /// <summary>
        /// Prints a concise list of documented Python API members for this object.
        /// </summary>
        [PythonAPI("Prints documented Python API members for this object.", Returns = "None")]
        public void help()
        {
            using (Py.GIL())
            using (PyObject builtins = Py.Import("builtins"))
            using (PyObject text = GetPythonHelpText().ToPython())
            {
                builtins.InvokeMethod("print", text);
            }
        }
        /// <summary>
        /// Returns a concise list of documented Python API members for this object.
        /// </summary>
        public string help_text()
        {
            return GetPythonHelpText();
        }
        public override string ToString()
        {
            return GetType().Name;
        }

        protected virtual IEnumerable<string> GetDynamicPythonMemberNames()
        {
            return new string[0];
        }
        protected virtual string ToFullString()
        {
            return ToString();
        }
        protected List<string> GetPythonMemberNames()
        {
            HashSet<string> names = new HashSet<string>();
            Type type = GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                if (property.GetCustomAttributes(typeof(PythonAPIAttribute), true).Length > 0)
                    names.Add(property.Name);
            }
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.IsSpecialName) continue;
                if (method.GetCustomAttributes(typeof(PythonAPIAttribute), true).Length > 0)
                    names.Add(method.Name);
            }
            foreach (string name in GetDynamicPythonMemberNames()) names.Add(name);
            List<string> sorted = names.ToList();
            sorted.Sort(StringComparer.OrdinalIgnoreCase);
            return sorted;
        }
        private string GetPythonHelpText()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(ToString());
            foreach (string line in GetPythonMemberHelpLines())
            {
                sb.Append('\n');
                sb.Append(line);
            }
            return sb.ToString();
        }
        private IEnumerable<string> GetPythonMemberHelpLines()
        {
            Type type = GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            List<string> lines = new List<string>();
            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                PythonAPIAttribute attr = (PythonAPIAttribute)property.GetCustomAttributes(typeof(PythonAPIAttribute), true)
                                                                    .FirstOrDefault();
                if (attr == null) continue;
                string returns = string.IsNullOrEmpty(attr.Returns) ? property.PropertyType.Name : attr.Returns;
                lines.Add("  " + property.Name + " -> " + returns + ": " + attr.Description);
            }
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.IsSpecialName) continue;
                PythonAPIAttribute attr = (PythonAPIAttribute)method.GetCustomAttributes(typeof(PythonAPIAttribute), true)
                                                                  .FirstOrDefault();
                if (attr == null) continue;
                string arguments = attr.Arguments;
                if (arguments == null)
                {
                    arguments = string.Join(", ", method.GetParameters().Select(p => p.Name));
                }
                string returns = string.IsNullOrEmpty(attr.Returns) ? "" : " -> " + attr.Returns;
                lines.Add("  " + method.Name + "(" + arguments + ")" + returns + ": " + attr.Description);
                if (!string.IsNullOrEmpty(attr.Example)) lines.Add("      " + attr.Example);
            }
            lines.Sort(StringComparer.OrdinalIgnoreCase);
            return lines;
        }
    }
}
