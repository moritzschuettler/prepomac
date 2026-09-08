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
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace PrePoMaxMcp
{
    // The connection to one PrePoMax instance. It reconnects on demand rather than holding the pipe
    // open forever, because the user may stop the server between two tool calls and an agent should
    // be told that plainly instead of seeing a dead connection.
    public class PipeClient
    {
        // Variables                                                                                                                
        private const int ConnectTimeout = 5000;
        private readonly string _pipeName;
        private NamedPipeClientStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;
        private int _nextId;


        // Properties                                                                                                               
        public bool IsConnected { get { return _stream != null && _stream.IsConnected; } }


        // Constructor                                                                                                              
        public PipeClient(string pipeName)
        {
            _pipeName = pipeName;
            _nextId = 0;
        }


        // Methods                                                                                                                  
        public object Call(string method, Dictionary<string, object> arguments)
        {
            _nextId++;
            //
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("jsonrpc", "2.0");
            writer.WriteValue("id", _nextId);
            writer.WriteValue("method", method);
            if (arguments != null && arguments.Count > 0)
            {
                writer.BeginObject("params");
                foreach (KeyValuePair<string, object> argument in arguments) WriteArgument(writer, argument);
                writer.EndObject();
            }
            writer.EndObject();
            //
            string line = Flatten(writer.ToString());
            // A connection left idle between two tool calls can be found broken, and reconnecting is
            // what this client is for. The request is only sent again when it never went out: one
            // that reached the server may already have changed the model
            for (int attempt = 0; ; attempt++)
            {
                bool isSent = false;
                try
                {
                    EnsureConnected();
                    _writer.WriteLine(line);
                    isSent = true;
                    string answer = _reader.ReadLine();
                    if (answer == null) throw new McpException("PrePoMax closed the connection.");
                    //
                    object document;
                    string error;
                    if (!JsonReader.TryParse(answer, out document, out error))
                        throw new McpException("PrePoMax answered with something that is not JSON: " + error);
                    return document;
                }
                catch (IOException exception)
                {
                    Close();
                    if (isSent || attempt > 0)
                        throw new McpException("The connection to PrePoMax failed: " + exception.Message);
                }
                catch (ObjectDisposedException)
                {
                    Close();
                    if (isSent || attempt > 0) throw new McpException("The connection to PrePoMax was closed.");
                }
            }
        }
        public void Close()
        {
            if (_reader != null) { try { _reader.Dispose(); } catch { } _reader = null; }
            if (_writer != null) { try { _writer.Dispose(); } catch { } _writer = null; }
            if (_stream != null) { try { _stream.Dispose(); } catch { } _stream = null; }
        }
        private void EnsureConnected()
        {
            if (IsConnected) return;
            //
            Close();
            _stream = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut);
            try
            {
                _stream.Connect(ConnectTimeout);
            }
            catch (Exception exception)
            {
                Close();
                throw new McpException("The automation server of PrePoMax could not be reached on the pipe " +
                                       _pipeName + ". It is off unless the user turned it on in " +
                                       "Tools - Automation Server. (" + exception.Message + ")");
            }
            UTF8Encoding encoding = new UTF8Encoding(false);
            _reader = new StreamReader(_stream, encoding);
            _writer = new StreamWriter(_stream, encoding);
            _writer.AutoFlush = true;
            _writer.NewLine = "\n";
        }
        private static void WriteArgument(JsonWriter writer, KeyValuePair<string, object> argument)
        {
            object value = argument.Value;
            if (value == null) writer.WriteRawValue(argument.Key, "null");
            else if (value is string) writer.WriteValue(argument.Key, (string)value);
            else if (value is bool) writer.WriteValue(argument.Key, (bool)value);
            else if (value is int) writer.WriteValue(argument.Key, (int)value);
            else if (value is double) writer.WriteValue(argument.Key, (double)value);
            else writer.WriteValue(argument.Key, value.ToString());
        }
        private static string Flatten(string text)
        {
            string[] lines = text.Replace("\r", "").Split('\n');
            StringBuilder line = new StringBuilder();
            for (int i = 0; i < lines.Length; i++) line.Append(lines[i].TrimStart());
            return line.ToString();
        }
        public static string GetPipeName(int processId)
        {
            return "PrePoMax." + processId.ToString(CultureInfo.InvariantCulture);
        }
    }
}
