// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace PrePoMax.Rpc
{
    // One connection. It reads a request per line and writes a response per line, and it never lets a
    // failure of one request end the connection: a client that sends nonsense is answered and asked
    // again, because it may well be an agent that will get the next one right.
    public class RpcSession
    {
        // Variables                                                                                                                
        private readonly RpcServer _server;
        private readonly NamedPipeServerStream _stream;
        private volatile bool _isClosed;


        // Properties                                                                                                               
        public bool IsClosed { get { return _isClosed; } }


        // Constructor                                                                                                              
        public RpcSession(RpcServer server, NamedPipeServerStream stream)
        {
            _server = server;
            _stream = stream;
            _isClosed = false;
        }


        // Methods                                                                                                                  
        public void Run()
        {
            try
            {
                UTF8Encoding encoding = new UTF8Encoding(false);
                // The reader and the writer are deliberately not disposed. Disposing a StreamWriter
                // flushes it, and on shutdown the stream it would flush into is the one that has just
                // been closed - the read ends at end of file, so that flush was the only thing left here
                // that threw. Nothing is lost by leaving it out, because every line is flushed as it is
                // written, and neither class has a finalizer that would flush later. The stream is
                // closed below, which is what has to happen
                StreamReader reader = new StreamReader(_stream, encoding);
                StreamWriter writer = new StreamWriter(_stream, encoding);
                writer.AutoFlush = true;
                writer.NewLine = "\n";
                //
                while (!_isClosed)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    if (line.Trim().Length == 0) continue;
                    //
                    RpcRequest request;
                    RpcResponse failure;
                    if (!RpcRequest.TryParse(line, out request, out failure))
                    {
                        if (failure != null) writer.WriteLine(failure.Line);
                        continue;
                    }
                    // A notification is not answered, so only shutdown is run as one: an exec sent without an id
                    // would change the model with no way to tell the client what it did, and would hold the turn
                    // while nobody waits for it. Any other notification is dropped, as the MCP endpoint drops it
                    if (request.IsNotification && request.Method != "shutdown") continue;
                    // The answer of a long call stays collectable with wait when the line carrying it cannot be
                    // written, because the client went away before it came
                    RpcDispatcher dispatcher = _server.GetDispatcher();
                    bool isDelivered = false;
                    dispatcher.BeginDelivery();
                    try
                    {
                        RpcResponse response = dispatcher.Dispatch(request);
                        // A notification is not answered, which is what makes shutdown possible
                        if (!request.IsNotification && response != null) writer.WriteLine(response.Line);
                        isDelivered = true;
                    }
                    finally
                    {
                        dispatcher.EndDelivery(isDelivered);
                    }
                }
            }
            catch (Exception)
            {
                // The client is gone, or the server closed the stream underneath the reader
            }
            finally
            {
                _isClosed = true;
                try { _stream.Dispose(); }
                catch { }
            }
        }
        // Closing the handle is what ends the read the session thread waits in, and it is enough on its
        // own: disconnecting first only adds a second operation that has to reach the same pipe. The
        // client is not asked and nothing waits for it to answer - a read pending on a pipe that is no
        // longer connected ends at end of file rather than with an error, whether the client said
        // goodbye, sat idle or went away without a word
        public void Close()
        {
            _isClosed = true;
            try { _stream.Dispose(); }
            catch { }
        }
    }
}
