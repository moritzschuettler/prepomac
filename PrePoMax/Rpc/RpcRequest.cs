// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;

namespace PrePoMax.Rpc
{
    // One request line of the automation protocol: JSON-RPC 2.0, named arguments only, no batches.
    // The id is kept as it arrived - a string or a number - because it has to be echoed unchanged.
    // The accessors are the ones a handler needs: a missing optional argument is its default value,
    // and a missing required one is an InvalidParams error naming the argument.
    public class RpcRequest
    {
        // Variables                                                                                                                
        public const string Version = "2.0";
        private readonly string _method;
        private readonly object _id;
        private readonly bool _hasId;
        private readonly Dictionary<string, object> _parameters;


        // Properties                                                                                                               
        public string Method { get { return _method; } }
        public object Id { get { return _id; } }
        public bool IsNotification { get { return !_hasId; } }


        // Constructor                                                                                                              
        private RpcRequest(string method, object id, bool hasId, Dictionary<string, object> parameters)
        {
            _method = method;
            _id = id;
            _hasId = hasId;
            _parameters = parameters == null ? new Dictionary<string, object>() : parameters;
        }


        // Static methods                                                                                                           
        // Reads one line. A line that is not a valid request produces the response that has to be sent
        // instead of it, which is why the failure is a response rather than a message.
        public static bool TryParse(string line, out RpcRequest request, out RpcResponse failure)
        {
            request = null;
            failure = null;
            object document;
            string error;
            if (!JsonReader.TryParse(line, out document, out error))
            {
                failure = RpcResponse.FromError(null, RpcErrorCode.ParseError, error);
                return false;
            }
            if (document is List<object>)
            {
                failure = RpcResponse.FromError(null, RpcErrorCode.InvalidRequest,
                                                "A batch of requests is not supported. Send one request per line.");
                return false;
            }
            Dictionary<string, object> items = document as Dictionary<string, object>;
            if (items == null)
            {
                failure = RpcResponse.FromError(null, RpcErrorCode.InvalidRequest, "The request must be an object.");
                return false;
            }
            // The id is read first, so that everything after it can be reported against the right id
            object id = null;
            bool hasId = false;
            if (items.ContainsKey("id") && items["id"] != null)
            {
                id = items["id"];
                if (!(id is string) && !(id is double))
                {
                    failure = RpcResponse.FromError(null, RpcErrorCode.InvalidRequest,
                                                    "The id must be a string or a number.");
                    return false;
                }
                hasId = true;
            }
            //
            string version = JsonReader.GetString(items, "jsonrpc", null);
            if (version != Version)
            {
                failure = RpcResponse.FromError(id, RpcErrorCode.InvalidRequest,
                                                "The jsonrpc member must be \"" + Version + "\".");
                return false;
            }
            string method = JsonReader.GetString(items, "method", null);
            if (string.IsNullOrEmpty(method))
            {
                failure = RpcResponse.FromError(id, RpcErrorCode.InvalidRequest,
                                                "The method member must be a non empty string.");
                return false;
            }
            Dictionary<string, object> parameters = null;
            if (items.ContainsKey("params") && items["params"] != null)
            {
                parameters = items["params"] as Dictionary<string, object>;
                if (parameters == null)
                {
                    failure = RpcResponse.FromError(id, RpcErrorCode.InvalidParams,
                                                    "The params member must be an object of named arguments.");
                    return false;
                }
            }
            //
            request = new RpcRequest(method, id, hasId, parameters);
            return true;
        }


        // Methods                                                                                                                  
        public bool Has(string name)
        {
            return _parameters.ContainsKey(name) && _parameters[name] != null;
        }
        public string GetString(string name, string defaultValue)
        {
            return JsonReader.GetString(_parameters, name, defaultValue);
        }
        public int GetInt(string name, int defaultValue)
        {
            return JsonReader.GetInt(_parameters, name, defaultValue);
        }
        public bool GetBool(string name, bool defaultValue)
        {
            return JsonReader.GetBool(_parameters, name, defaultValue);
        }
        public string RequireString(string name)
        {
            object value;
            if (!_parameters.TryGetValue(name, out value) || value == null)
                throw new RpcException(RpcErrorCode.InvalidParams, "The argument " + name + " is required.");
            if (!(value is string))
                throw new RpcException(RpcErrorCode.InvalidParams, "The argument " + name + " must be a string.");
            return (string)value;
        }
        public string RequireOneOf(string name, string[] allowed)
        {
            string value = RequireString(name);
            for (int i = 0; i < allowed.Length; i++)
            {
                if (string.Equals(value, allowed[i], StringComparison.Ordinal)) return value;
            }
            throw new RpcException(RpcErrorCode.InvalidParams, "The argument " + name + " must be one of " +
                                                              string.Join(", ", allowed) + ".");
        }
    }
}
