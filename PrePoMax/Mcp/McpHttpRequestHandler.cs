// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using PrePoMax.Rpc;
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
        private bool _isClientRequest;


        // Constructor                                                                                                              
        public McpHttpRequestHandler(McpHttpServer server, HttpListenerContext context)
        {
            _server = server;
            _context = context;
            _isClientRequest = false;
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
                TryWriteText(500, "The endpoint failed to serve the request: " + exception.Message);
            }
            finally
            {
                if (_isClientRequest) _server.EndClientRequest();
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
                WriteText(401, "The MCP endpoint needs the current access token of this PrePoMax session in an " +
                               "Authorization: Bearer header. Copy it from Tools - Settings - Automation, Access " +
                               "token, into the client configuration: it may have been generated again since the " +
                               "client was set up, and a newly unpacked PrePoMax folder starts without one.");
                return;
            }
            _isClientRequest = true;
            _server.BeginClientRequest();
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
                DrainBody(request);
                WriteText(413, "The request body must not exceed " + MaximumBodyLength + " bytes.");
                return;
            }
            string body;
            if (!TryReadBody(request, out body))
            {
                DrainBody(request);
                WriteText(413, "The request body must not exceed " + MaximumBodyLength + " bytes.");
                return;
            }
            // The answer of a long call is given up only once the response carrying it has been written: a
            // client whose own request timeout ran out before the wait did is gone, and collects it with get_call
            RpcDispatcher dispatcher = _server.Server.GetDispatcher();
            bool isDelivered = false;
            if (dispatcher != null) dispatcher.BeginDelivery();
            try
            {
                string answer = _server.HandleProtocol(body);
                // A notification is not answered, which is what the protocol says and what a client that
                // sends notifications/initialized expects
                if (answer == null)
                {
                    _context.Response.StatusCode = 202;
                    WriteBody(new byte[0], null);
                }
                else WriteBody(Encoding.UTF8.GetBytes(answer), JsonContentType + "; charset=utf-8");
                _context.Response.Close();
                isDelivered = true;
            }
            finally
            {
                if (dispatcher != null) dispatcher.EndDelivery(isDelivered);
            }
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
        // chunked request declares none. It is decoded as UTF-8 whatever the request says: JSON exchanged
        // between systems is UTF-8 (RFC 8259), and the encoding .NET Framework assumes for a Content-Type
        // without a charset is the ANSI code page, which turned every character outside ASCII into another
        // one. A byte order mark is dropped rather than read as part of the document
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
                byte[] bytes = content.ToArray();
                int start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
                body = new UTF8Encoding(false).GetString(bytes, start, bytes.Length - start);
            }
            return true;
        }
        // The body of a refused request is read to its end, up to a limit, before the answer is sent: a socket closed
        // while the client is still sending resets the connection on macOS and Linux, and the client then never
        // reads the answer
        private static void DrainBody(HttpListenerRequest request)
        {
            const long MaximumDrainLength = 64L * 1024 * 1024;
            try
            {
                byte[] buffer = new byte[64 * 1024];
                long total = 0;
                int count;
                while (total < MaximumDrainLength &&
                       (count = request.InputStream.Read(buffer, 0, buffer.Length)) > 0) total += count;
            }
            catch (Exception)
            {
                // The client is gone
            }
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
