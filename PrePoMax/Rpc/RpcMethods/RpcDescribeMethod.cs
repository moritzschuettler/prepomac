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

namespace PrePoMax.Rpc
{
    // How an agent learns the API instead of being told it. The path is resolved against the live
    // model, so pmx.model.steps['Step-1'].loads describes what is actually there, and a collection
    // reports the names it holds right now. It is read through RpcDescribePath, never evaluated, so a
    // path that is not names, attributes and literal indexes runs nothing.
    //
    // A name that is not a path is taken as an API type name, so the half of the API that hangs off
    // something the model does not have yet can still be read. A type name followed by a member path
    // - Result.mesh - is walked through the declared member types for the same reason. The path is
    // tried first: a mistyped path is reported as a path, not as a missing type.
    public static class RpcDescribeMethod
    {
        // Variables                                                                                                                
        // The arguments the method takes; any other is refused before anything is done
        public static readonly string[] Parameters = { "path" };
        private const string RootPath = "pmx";


        // Static methods                                                                                                           
        public static string Execute(RpcRequest request, RpcServer server)
        {
            string path = request.GetString("path", RootPath);
            if (string.IsNullOrEmpty(path)) path = RootPath;
            //
            server.Form.PrepareAutomationPythonEngine();
            string error;
            object target = Resolve(path, out error);
            if (error == null && target is BaseAPI) return RpcDescribeBuilder.Build(path, target);
            // A path that ends at a method or a property evaluates to a bound method or a value, neither of which
            // carries the metadata; the member is described from the type that owns it
            string member = DescribeMember(path);
            if (member != null) return member;
            if (error == null) return RpcDescribeBuilder.Build(path, target);
            //
            List<Type> types = RpcApiTypeLocator.Find(path);
            if (types.Count == 1) return RpcDescribeBuilder.BuildForType(RpcApiTypeLocator.GetShortName(types[0]),
                                                                        types[0]);
            if (types.Count > 1)
            {
                throw new RpcException(RpcErrorCode.InvalidParams, "The name " + path + " is shared by " +
                                                                   types.Count + " API types. Ask for one of " +
                                                                   RpcApiTypeLocator.GetNames(types) + ".");
            }
            //
            string resolvedName;
            string memberError;
            Type memberType = RpcApiTypeLocator.FindByMemberPath(path, out resolvedName, out memberError);
            if (memberType != null) return RpcDescribeBuilder.BuildForType(resolvedName, memberType);
            //
            throw new RpcException(RpcErrorCode.InvalidParams, "The path " + path + " cannot be resolved: " +
                                                               error.TrimEnd() + " It is not an API type name " +
                                                               "either." +
                                                               (memberError == null ? "" : " " + memberError) +
                                                               GetSuggestion(path));
        }
        private static object Resolve(string path, out string error)
        {
            RpcDescribePath parsed = RpcDescribePath.Parse(path, out error);
            if (parsed == null) return null;
            return parsed.Resolve(out error);
        }
        private static string DescribeMember(string path)
        {
            string head;
            string name;
            if (!SplitLastName(path, out head, out name)) return null;
            //
            Type ownerType = ResolveType(head);
            return ownerType == null ? null : RpcDescribeBuilder.BuildForMember(path, ownerType, name);
        }
        // The type a path, an API type name or a type name with a member path stands for, or null
        private static Type ResolveType(string path)
        {
            string error;
            object target = Resolve(path, out error);
            if (error == null && target is BaseAPI) return target.GetType();
            //
            List<Type> types = RpcApiTypeLocator.Find(path);
            if (types.Count == 1) return types[0];
            if (types.Count > 1) return null;
            //
            string resolvedName;
            string memberError;
            return RpcApiTypeLocator.FindByMemberPath(path, out resolvedName, out memberError);
        }
        private static bool SplitLastName(string path, out string head, out string name)
        {
            head = null;
            name = null;
            int dot = path.LastIndexOf('.');
            if (dot <= 0 || dot == path.Length - 1) return false;
            //
            name = path.Substring(dot + 1);
            foreach (char character in name)
            {
                if (!char.IsLetterOrDigit(character) && character != '_') return false;
            }
            head = path.Substring(0, dot);
            return true;
        }
        // A mistyped name is most often a dot written as an underscore or a letter out of place, so the names the
        // path could have continued with are offered, nearest first
        private static string GetSuggestion(string path)
        {
            string head;
            string name;
            if (!SplitLastName(path, out head, out name)) return "";
            Type ownerType = ResolveType(head);
            if (ownerType == null) return "";
            //
            string wanted = Normalize(name);
            List<KeyValuePair<int, string>> matches = new List<KeyValuePair<int, string>>();
            foreach (string candidate in RpcDescribeBuilder.GetReachableNames(ownerType))
            {
                string normalized = Normalize(candidate);
                int distance = normalized == wanted ? 0 : GetDistance(wanted, normalized);
                if (distance <= Math.Max(1, wanted.Length / 4))
                    matches.Add(new KeyValuePair<int, string>(distance, head + "." + candidate));
            }
            if (matches.Count == 0) return "";
            //
            matches.Sort((first, second) => first.Key != second.Key
                                            ? first.Key.CompareTo(second.Key)
                                            : string.CompareOrdinal(first.Value, second.Value));
            List<string> paths = new List<string>();
            for (int i = 0; i < matches.Count && i < 3; i++) paths.Add(matches[i].Value);
            return " Did you mean " + string.Join(" or ", paths.ToArray()) + "?";
        }
        private static string Normalize(string name)
        {
            return name.Replace("_", "").Replace(".", "").ToLowerInvariant();
        }
        private static int GetDistance(string first, string second)
        {
            int[] previous = new int[second.Length + 1];
            int[] current = new int[second.Length + 1];
            for (int j = 0; j <= second.Length; j++) previous[j] = j;
            for (int i = 1; i <= first.Length; i++)
            {
                current[0] = i;
                for (int j = 1; j <= second.Length; j++)
                {
                    int cost = first[i - 1] == second[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }
                int[] swap = previous;
                previous = current;
                current = swap;
            }
            return previous[second.Length];
        }
    }
}
