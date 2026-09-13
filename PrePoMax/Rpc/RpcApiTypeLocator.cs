// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PrePoMax.Rpc
{
    // Finds an API type by the name a caller would use for it. It exists because half of the API is
    // reachable only through something the model does not have yet: the loads of a step cannot be
    // described before a step exists, and creating one to read its schema is a change to the user's
    // model made for no other reason.
    //
    // A name is taken as written, as the API class name, or qualified by its namespace tail when two
    // areas share it - the model and the results sides both own Mesh, Nodes, Elements and Parts, so
    // 'Nodes' is ambiguous and 'Model.Nodes' is not.
    public static class RpcApiTypeLocator
    {
        // Static methods                                                                                                           
        public static List<Type> Find(string name)
        {
            List<Type> found = FindByName(name);
            if (found.Count > 0 || string.IsNullOrEmpty(name)) return found;
            // The documentation name of a type is one word, and a caller who reaches it through the model
            // tree writes that word the way the tree reads - Mesh.SetupItems for MeshSetupItems. That is the
            // same type spelled with separators, not a second one, so it resolves rather than being refused
            // with a message about a namespace the caller never meant
            string joined = name.Replace(".", "").Trim();
            if (joined.Length == 0 || joined == name.Trim()) return found;
            //
            return FindByName(joined);
        }
        private static List<Type> FindByName(string name)
        {
            List<Type> found = new List<Type>();
            if (string.IsNullOrEmpty(name)) return found;
            //
            string wanted = name.Trim();
            string requiredNamespaceTail = null;
            int separator = wanted.LastIndexOf('.');
            if (separator > 0)
            {
                requiredNamespaceTail = wanted.Substring(0, separator);
                wanted = wanted.Substring(separator + 1);
            }
            foreach (Type type in typeof(PrePoMaxAPI).Assembly.GetTypes())
            {
                if (!type.IsPublic || !type.IsClass) continue;
                if (!IsSameName(type.Name, wanted)) continue;
                if (requiredNamespaceTail != null && !EndsWithNamespace(type.Namespace, requiredNamespaceTail))
                    continue;
                found.Add(type);
            }
            //
            return found;
        }
        public static string GetNames(List<Type> types)
        {
            List<string> names = new List<string>();
            foreach (Type type in types) names.Add(GetShortName(type));
            return string.Join(", ", names.ToArray());
        }
        // The name the API documentation uses, which is the class name without the API suffix, made
        // unambiguous by the namespace tail
        public static string GetShortName(Type type)
        {
            string name = type.Name.EndsWith("API", StringComparison.Ordinal) && type.Name.Length > 3
                          ? type.Name.Substring(0, type.Name.Length - 3) : type.Name;
            if (type.Namespace != null && type.Namespace.EndsWith(".Model", StringComparison.Ordinal))
                return "Model." + name;
            //
            return name;
        }
        private static bool IsSameName(string typeName, string wanted)
        {
            if (string.Equals(typeName, wanted, StringComparison.OrdinalIgnoreCase)) return true;
            return string.Equals(typeName, wanted + "API", StringComparison.OrdinalIgnoreCase);
        }
        private static bool EndsWithNamespace(string typeNamespace, string tail)
        {
            if (typeNamespace == null) return false;
            if (string.Equals(typeNamespace, tail, StringComparison.OrdinalIgnoreCase)) return true;
            return typeNamespace.EndsWith("." + tail, StringComparison.OrdinalIgnoreCase);
        }
    }
}
