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
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace PrePoMax.Mcp
{
    // One request, and the gates it has to pass before any of it reaches the model. A loopback port
    // is not a named pipe: every program on the machine can reach it, and so can every page in the
    // user's browser, so the endpoint authenticates its callers itself.
    //
    // Three gates, and each stops something the other two do not:
    //
    //   the binding    only the two loopback prefixes are claimed, and the request is checked for
    //                  being local again here, so a prefix mistake cannot open the endpoint silently;
    //   the token      a bearer token the user configured, compared in constant time, which a page in
    //                  the browser cannot read because it is in a file;
    //   the origin     any request carrying an Origin header is refused, and no Access-Control header
    //                  is ever answered with, so the preflight a cross origin POST needs can never
    //                  succeed. This is what stops DNS rebinding: a page told that its own name
    //                  resolves to 127.0.0.1 still has to send its own origin, and still cannot read
    //                  the token.
    //
    // A failure of the protocol is a 200 carrying a JSON-RPC error, because the protocol was reached.
    // A failure of a gate is an HTTP status, because it never was.
    internal class McpHttpRequestHandler
    {
        // Variables                                                                                                                
        private const int MaximumBodyLength = 4 * 1024 * 1024;
        private const string JsonContentType = "application/json";
        private readonly McpHttpServer _server;
        private readonly HttpListenerContext _context;


        // Constructor                                                                                                              
        public McpHttpRequestHandler(McpHttpServer server, HttpListenerContext context)
        {
            _server = server;
            _context = context;
        }


        // Methods                                                                                                                  
        public void Serve()
        {
            try
            {
                ServeRequest();
            }
            catch (Exception exception)
            {
                // A request that fails must not take the endpoint down with it, and the caller is
                // told rather than left waiting for a response that will never come
                _server.ReportError("An MCP request failed: " + exception.Message);
                TryWriteText(500, "The endpoint failed to serve the request.");
            }
            finally
            {
                try { _context.Response.Close(); }
                catch { }
            }
        }
        private void ServeRequest()
        {
            HttpListenerRequest request = _context.Request;
            // http.sys already refused anything whose Host is not one of the two loopback names, so
            // this is the check that survives a change to the prefixes
            if (!request.IsLocal || !IsLoopback(request.RemoteEndPoint))
            {
                WriteText(403, "The MCP endpoint serves loopback requests only.");
                return;
            }
            if (request.Headers["Origin"] != null)
            {
                WriteText(403, "The MCP endpoint refuses a request that carries an Origin header. It is not " +
                               "reachable from a browser.");
                return;
            }
            if (!IsLoopbackHost(request.UserHostName))
            {
                WriteText(403, "The MCP endpoint answers to 127.0.0.1 and localhost only.");
                return;
            }
            string method = request.HttpMethod;
            if (string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                // No preflight is ever granted, so a cross origin POST cannot be made
                WriteText(403, "The MCP endpoint grants no cross origin access.");
                return;
            }
            if (!string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                _context.Response.AddHeader("Allow", "POST");
                WriteText(405, "The MCP endpoint takes POST. It serves no event stream, because nothing in its " +
                               "surface pushes: every answer is the response to a request.");
                return;
            }
            if (!IsAuthorized(request))
            {
                _context.Response.AddHeader("WWW-Authenticate", "Bearer");
                WriteText(401, "The MCP endpoint needs the access token of this PrePoMax session in an " +
                               "Authorization: Bearer header.");
                return;
            }
            if (!IsEndpointPath(request.Url))
            {
                WriteText(404, "The MCP endpoint is served at " + McpHttpServer.EndpointPath + ".");
                return;
            }
            if (!IsJsonContentType(request.ContentType))
            {
                WriteText(415, "The request body must be " + JsonContentType + ".");
                return;
            }
            if (request.ContentLength64 > MaximumBodyLength)
            {
                WriteText(413, "The request body must not exceed " + MaximumBodyLength + " bytes.");
                return;
            }
            string body;
            if (!TryReadBody(request, out body))
            {
                WriteText(413, "The request body must not exceed " + MaximumBodyLength + " bytes.");
                return;
            }
            string answer = _server.HandleProtocol(body);
            // A notification is not answered, which is what the protocol says and what a client that
            // sends notifications/initialized expects
            if (answer == null)
            {
                _context.Response.StatusCode = 202;
                WriteBody(new byte[0], null);
                return;
            }
            WriteBody(Encoding.UTF8.GetBytes(answer), JsonContentType + "; charset=utf-8");
        }
        private bool IsAuthorized(HttpListenerRequest request)
        {
            string header = request.Headers["Authorization"];
            if (header == null) return false;
            //
            const string scheme = "Bearer ";
            if (!header.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)) return false;
            return McpAccessToken.IsMatch(_server.AccessToken, header.Substring(scheme.Length).Trim());
        }
        // The body is read through a bounded copy rather than trusted to declare its length, because a
        // chunked request declares none
        private bool TryReadBody(HttpListenerRequest request, out string body)
        {
            body = null;
            byte[] buffer = new byte[8192];
            using (MemoryStream content = new MemoryStream())
            using (Stream stream = request.InputStream)
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0) break;
                    if (content.Length + read > MaximumBodyLength) return false;
                    content.Write(buffer, 0, read);
                }
                Encoding encoding = request.ContentEncoding == null ? Encoding.UTF8 : request.ContentEncoding;
                body = encoding.GetString(content.ToArray());
            }
            return true;
        }
        private void WriteText(int statusCode, string message)
        {
            _context.Response.StatusCode = statusCode;
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("error", message);
            writer.EndObject();
            WriteBody(Encoding.UTF8.GetBytes(writer.ToString()), JsonContentType + "; charset=utf-8");
        }
        private void TryWriteText(int statusCode, string message)
        {
            try
            {
                WriteText(statusCode, message);
            }
            catch (Exception)
            {
                // The response was already begun, or the caller is gone
            }
        }
        private void WriteBody(byte[] content, string contentType)
        {
            HttpListenerResponse response = _context.Response;
            if (contentType != null) response.ContentType = contentType;
            // Nothing here is ever cached, and no cross origin access is ever granted: an
            // Access-Control-Allow-Origin header on this endpoint would undo the Origin refusal above
            response.AddHeader("Cache-Control", "no-store");
            response.ContentLength64 = content.Length;
            if (content.Length > 0) response.OutputStream.Write(content, 0, content.Length);
        }
        private static bool IsEndpointPath(Uri url)
        {
            if (url == null) return false;
            //
            string path = url.AbsolutePath;
            if (path == null) return false;
            if (path.Length > 1 && path.EndsWith("/", StringComparison.Ordinal))
                path = path.Substring(0, path.Length - 1);
            return string.Equals(path, McpHttpServer.EndpointPath, StringComparison.OrdinalIgnoreCase);
        }
        private static bool IsJsonContentType(string contentType)
        {
            if (contentType == null) return false;
            //
            int end = contentType.IndexOf(';');
            string type = end < 0 ? contentType : contentType.Substring(0, end);
            return string.Equals(type.Trim(), JsonContentType, StringComparison.OrdinalIgnoreCase);
        }
        private static bool IsLoopback(System.Net.IPEndPoint endPoint)
        {
            return endPoint != null && endPoint.Address != null && IPAddress.IsLoopback(endPoint.Address);
        }
        // The Host header as the client sent it, port and all
        private static bool IsLoopbackHost(string host)
        {
            if (string.IsNullOrEmpty(host)) return false;
            //
            int colon = host.LastIndexOf(':');
            string name = colon < 0 ? host : host.Substring(0, colon);
            return string.Equals(name, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "localhost", StringComparison.OrdinalIgnoreCase);
        }
    }
}
