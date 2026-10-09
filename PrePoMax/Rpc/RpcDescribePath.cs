// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using PrePoMax.Python;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PrePoMax.Rpc
{
    // A path describe resolves against the live model, read without evaluating it. A path is a name
    // followed by attributes and indexes - pmx.model.steps['Step-1'].loads - and nothing else: an
    // index is a string or an integer literal, and a call, an operator or any other expression is
    // refused before anything runs. The path is then walked one step at a time through pythonnet,
    // with the getattr and the indexing Python itself would do, so describing a path can read a
    // property but can never run code the caller wrote.
    public class RpcDescribePath
    {
        // Variables                                                                                                                
        private readonly string _root;
        // An attribute step has a name and no key, an index step a key and no name
        private readonly List<string> _names;
        private readonly List<object> _keys;


        // Constructor                                                                                                              
        private RpcDescribePath(string root)
        {
            _root = root;
            _names = new List<string>();
            _keys = new List<object>();
        }


        // Static methods                                                                                                           
        // The parsed path, or null with the reason in error
        public static RpcDescribePath Parse(string path, out string error)
        {
            error = null;
            int position = 0;
            string root = ReadName(path, ref position);
            if (root == null) return Refuse(path, "it does not start with a name", out error);
            //
            RpcDescribePath parsed = new RpcDescribePath(root);
            while (position < path.Length)
            {
                char character = path[position];
                if (character == '.')
                {
                    position++;
                    string name = ReadName(path, ref position);
                    if (name == null) return Refuse(path, "a dot is not followed by a name", out error);
                    parsed._names.Add(name);
                    parsed._keys.Add(null);
                }
                else if (character == '[')
                {
                    position++;
                    SkipSpaces(path, ref position);
                    object key = ReadLiteral(path, ref position);
                    if (key == null) return Refuse(path, "an index is not a string or an integer literal", out error);
                    SkipSpaces(path, ref position);
                    if (position >= path.Length || path[position] != ']')
                        return Refuse(path, "an index is not closed by ]", out error);
                    position++;
                    parsed._names.Add(null);
                    parsed._keys.Add(key);
                }
                else
                {
                    return Refuse(path, "'" + character + "' at position " +
                                        (position + 1).ToString(CultureInfo.InvariantCulture) + " is neither a dot nor [",
                                  out error);
                }
            }
            return parsed;
        }
        private static RpcDescribePath Refuse(string path, string reason, out string error)
        {
            error = "The path " + path + " is not a name followed by .attribute and ['name'] or [index] steps - " +
                    reason + ". A live path is read, never evaluated, so a call or any other expression is " +
                    "not described; describe the member instead, for example pmx.model.parts.get_names.";
            return null;
        }
        private static string ReadName(string path, ref int position)
        {
            int start = position;
            while (position < path.Length)
            {
                char character = path[position];
                bool isFirst = position == start;
                if (char.IsLetter(character) || character == '_' || (!isFirst && char.IsDigit(character))) position++;
                else break;
            }
            return position == start ? null : path.Substring(start, position - start);
        }
        private static void SkipSpaces(string path, ref int position)
        {
            while (position < path.Length && path[position] == ' ') position++;
        }
        // A quoted string with the escapes a name can need, or an integer. Anything else is null
        private static object ReadLiteral(string path, ref int position)
        {
            if (position >= path.Length) return null;
            //
            char quote = path[position];
            if (quote == '\'' || quote == '"')
            {
                StringBuilder text = new StringBuilder();
                for (int i = position + 1; i < path.Length; i++)
                {
                    char character = path[i];
                    if (character == quote)
                    {
                        position = i + 1;
                        return text.ToString();
                    }
                    if (character == '\\')
                    {
                        if (i + 1 >= path.Length) return null;
                        char escaped = path[i + 1];
                        if (escaped != '\\' && escaped != '\'' && escaped != '"') return null;
                        text.Append(escaped);
                        i++;
                    }
                    else text.Append(character);
                }
                return null;
            }
            int start = position;
            if (path[position] == '-') position++;
            while (position < path.Length && char.IsDigit(path[position])) position++;
            int value;
            if (int.TryParse(path.Substring(start, position - start), NumberStyles.AllowLeadingSign,
                             CultureInfo.InvariantCulture, out value))
                return value;
            position = start;
            return null;
        }


        // Methods                                                                                                                  
        // The object the path ends at, or null with the reason in error. The root is a bare name, so
        // looking it up runs nothing
        public object Resolve(out string error)
        {
            object root = PythonEngineManager.EvaluateToApiObject(_root, out error);
            if (error != null || _names.Count == 0) return root;
            if (root == null)
            {
                error = "The name " + _root + " is not an object that can be described.";
                return null;
            }
            //
            using (Py.GIL())
            {
                PyObject current = root.ToPython();
                try
                {
                    for (int i = 0; i < _names.Count; i++)
                    {
                        PyObject next;
                        if (_names[i] != null) next = current.GetAttr(_names[i]);
                        else if (_keys[i] is int) next = current.GetItem((int)_keys[i]);
                        else next = current.GetItem((string)_keys[i]);
                        current.Dispose();
                        current = next;
                    }
                    try
                    {
                        return current.AsManagedObject(typeof(object));
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
                catch (PythonException exception)
                {
                    error = exception.Message;
                    return null;
                }
                finally
                {
                    current.Dispose();
                }
            }
        }
    }
}
