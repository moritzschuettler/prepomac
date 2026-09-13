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
        // A type name followed by a member path - Result.mesh, Result.steps.last().increments - names
        // the type a member returns, so the API below a type can be read without an object of it: the
        // results side before an analysis has run. The longest head that is one type is taken and the
        // tail is walked through the declared member types. A member declared as object cannot be
        // walked, and the error says to describe it as a live path instead
        public static Type FindByMemberPath(string path, out string resolvedName, out string error)
        {
            resolvedName = null;
            error = null;
            if (string.IsNullOrEmpty(path)) return null;
            //
            string[] segments = path.Trim().Split('.');
            for (int count = segments.Length - 1; count >= 1; count--)
            {
                string head = string.Join(".", segments, 0, count);
                List<Type> heads = Find(head);
                if (heads.Count == 0) continue;
                if (heads.Count > 1)
                {
                    error = "Its head " + head + " is shared by " + heads.Count + " API types. Ask for one of " +
                            GetNames(heads) + ".";
                    return null;
                }
                //
                Type type = heads[0];
                string name = GetShortName(type);
                for (int i = count; i < segments.Length; i++)
                {
                    type = WalkMember(type, segments[i], out error);
                    if (type == null) return null;
                    name += "." + segments[i].Trim();
                }
                resolvedName = name;
                return type;
            }
            //
            return null;
        }
        private static Type WalkMember(Type type, string segment, out string error)
        {
            error = null;
            string memberName = segment.Trim();
            int suffix = memberName.IndexOfAny(new char[] { '(', '[' });
            if (suffix >= 0) memberName = memberName.Substring(0, suffix).Trim();
            //
            Type memberType = GetMemberType(type, memberName);
            if (memberType == null)
            {
                error = GetShortName(type) + " has no member " + memberName + ".";
                return null;
            }
            if (segment.IndexOf('[') >= 0)
            {
                Type itemType = GetItemType(memberType);
                if (itemType == null)
                {
                    error = "The items of " + GetShortName(memberType) + " are not declared with one type, so an " +
                            "item cannot be described by name. Describe it as a live path instead.";
                    return null;
                }
                memberType = itemType;
            }
            if (!typeof(BaseAPI).IsAssignableFrom(memberType))
            {
                error = "The member " + memberName + " of " + GetShortName(type) + " is declared as " +
                        memberType.Name + ", so what it returns cannot be described by name. Describe it as a " +
                        "live path instead.";
                return null;
            }
            //
            return memberType;
        }
        private static Type GetMemberType(Type type, string memberName)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                if (property.GetIndexParameters().Length > 0) continue;
                if (string.Equals(property.Name, memberName, StringComparison.OrdinalIgnoreCase))
                    return property.PropertyType;
            }
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.IsSpecialName) continue;
                if (string.Equals(method.Name, memberName, StringComparison.OrdinalIgnoreCase)) return method.ReturnType;
            }
            //
            return null;
        }
        private static Type GetItemType(Type type)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                if (property.GetIndexParameters().Length > 0 && typeof(BaseAPI).IsAssignableFrom(property.PropertyType))
                    return property.PropertyType;
            }
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.Name == "get" && typeof(BaseAPI).IsAssignableFrom(method.ReturnType)) return method.ReturnType;
            }
            //
            return null;
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
