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
using System.Globalization;

namespace PrePoMax.Rpc
{
    // One request line of the automation protocol: JSON-RPC 2.0, named arguments only, no batches.
    // The id is kept as it arrived - a string or a number - because it has to be echoed unchanged.
    // The accessors are the ones a handler needs: a missing optional argument is its default value,
    // and a missing required one, or one of the wrong type, is an InvalidParams error naming the argument.
    public class RpcRequest
    {
        // Variables                                                                                                                
        public const string Version = "2.0";
        private readonly string _method;
        private readonly object _id;
        private readonly bool _hasId;
        private readonly Dictionary<string, object> _parameters;
        private string _echoNote;


        // Properties                                                                                                               
        public string Method { get { return _method; } }
        public object Id { get { return _id; } }
        public bool IsNotification { get { return !_hasId; } }
        // A one line note the Python panel shows instead of the source an exec runs. It is never read from
        // a request line, only set in process by a tool that runs a fixed script of its own, so the source
        // an agent writes is always echoed in full
        public string EchoNote { get { return _echoNote; } set { _echoNote = value; } }


        // Constructor                                                                                                              
        private RpcRequest(string method, object id, bool hasId, Dictionary<string, object> parameters)
        {
            _method = method;
            _id = id;
            _hasId = hasId;
            _parameters = parameters == null ? new Dictionary<string, object>() : parameters;
            _echoNote = null;
        }


        // Static methods                                                                                                           
        // Builds a request in process, for a caller that has a method and named arguments rather than
        // a line. It is not a notification: a caller that waits for the answer has an identity, even
        // when nothing writes it back out.
        public static RpcRequest Create(string method, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(method)) throw new ArgumentNullException("method");
            return new RpcRequest(method, null, true, parameters);
        }
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
            // The id is read first, so that everything after it can be reported against the right id. A null id
            // is neither a request that can be told from another nor a notification, so it is refused, the way
            // the MCP endpoint refuses it
            object id = null;
            bool hasId = false;
            if (items.ContainsKey("id") && items["id"] == null)
            {
                failure = RpcResponse.FromError(null, RpcErrorCode.InvalidRequest,
                                                "The id must be a string or a number, not null. Leave it out only " +
                                                "for shutdown, the one method run as a notification.");
                return false;
            }
            if (items.ContainsKey("id"))
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
        // A value of the wrong type is refused rather than read as the default, because the call would
        // otherwise run as if the argument had not been given: "true" would be false and "10" the default
        public string GetString(string name, string defaultValue)
        {
            object value;
            if (!_parameters.TryGetValue(name, out value) || value == null) return defaultValue;
            return ToStringValue(name, value);
        }
        public int GetInt(string name, int defaultValue)
        {
            object value;
            if (!_parameters.TryGetValue(name, out value) || value == null) return defaultValue;
            return ToIntValue(name, value);
        }
        public bool GetBool(string name, bool defaultValue)
        {
            object value;
            if (!_parameters.TryGetValue(name, out value) || value == null) return defaultValue;
            if (!(value is bool)) throw new RpcException(RpcErrorCode.InvalidParams, GetTypeRefusal(name, "true or false", value));
            return (bool)value;
        }
        public string RequireString(string name)
        {
            object value;
            if (!_parameters.TryGetValue(name, out value) || value == null)
                throw new RpcException(RpcErrorCode.InvalidParams, "The argument " + name + " is required.");
            return ToStringValue(name, value);
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
        private static string ToStringValue(string name, object value)
        {
            if (!(value is string)) throw new RpcException(RpcErrorCode.InvalidParams, GetTypeRefusal(name, "a string", value));
            return (string)value;
        }
        // Parsed JSON holds every number as a double, while parameters built in process hold their integers as
        // they are
        private static int ToIntValue(string name, object value)
        {
            if (value is int) return (int)value;
            if (value is long && (long)value >= int.MinValue && (long)value <= int.MaxValue) return (int)(long)value;
            if (value is double)
            {
                double number = (double)value;
                if (number == Math.Floor(number) && number >= int.MinValue && number <= int.MaxValue) return (int)number;
            }
            throw new RpcException(RpcErrorCode.InvalidParams, GetTypeRefusal(name, "a whole number", value));
        }
        private static string GetTypeRefusal(string name, string expected, object value)
        {
            string given;
            if (value is string) given = "the string \"" + (string)value + "\"";
            else if (value is bool) given = (bool)value ? "true" : "false";
            else if (value is double) given = ((double)value).ToString("R", CultureInfo.InvariantCulture);
            else if (value is List<object>) given = "an array";
            else if (value is Dictionary<string, object>) given = "an object";
            else given = Convert.ToString(value, CultureInfo.InvariantCulture);
            return "The argument " + name + " must be " + expected + ", not " + given + ". Nothing was done.";
        }
    }
}
