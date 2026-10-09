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
using System.Diagnostics;
using System.IO;

namespace PrePoMax.Rpc
{
    // Finds the instances that are listening. Each one writes a file named after its process id when
    // its server starts and removes it when the server stops, so a file left by a crash is recognized
    // rather than trusted: the process it names no longer exists.
    //
    // The reader lives next to the writer because the only thing that reads these files is now the
    // application itself, answering list_sessions for an agent that has reached one instance and asks
    // what else is open. The folder is per user, so it holds every portable copy the user runs.
    public static class RpcSessionInfoReader
    {
        // Static methods                                                                                                           
        public static List<RpcSessionInfo> GetSessions()
        {
            List<RpcSessionInfo> sessions = new List<RpcSessionInfo>();
            string directory = RpcSessionInfoWriter.GetSessionsDirectory();
            if (!Directory.Exists(directory)) return sessions;
            //
            string[] fileNames;
            try
            {
                fileNames = Directory.GetFiles(directory, "*.json");
            }
            catch (Exception)
            {
                return sessions;
            }
            foreach (string fileName in fileNames)
            {
                RpcSessionInfo session = Read(fileName);
                if (session == null) continue;
                if (!IsAlive(session.ProcessId))
                {
                    RemoveStale(fileName);
                    continue;
                }
                sessions.Add(session);
            }
            return sessions;
        }
        public static RpcSessionInfo Find(int processId)
        {
            foreach (RpcSessionInfo session in GetSessions())
            {
                if (session.ProcessId == processId) return session;
            }
            return null;
        }
        private static RpcSessionInfo Read(string fileName)
        {
            try
            {
                object document;
                string error;
                if (!JsonReader.TryParse(File.ReadAllText(fileName), out document, out error)) return null;
                //
                int processId = JsonReader.GetInt(document, "processId", 0);
                if (processId <= 0) return null;
                //
                return new RpcSessionInfo(processId,
                                          JsonReader.GetString(document, "pipeName", ""),
                                          JsonReader.GetString(document, "url", ""),
                                          JsonReader.GetInt(document, "port", 0),
                                          JsonReader.GetString(document, "token", ""),
                                          JsonReader.GetString(document, "protocolVersion", ""),
                                          JsonReader.GetString(document, "applicationVersion", ""),
                                          JsonReader.GetString(document, "modelFileName", ""),
                                          JsonReader.GetString(document, "startTime", ""));
            }
            catch (Exception)
            {
                return null;
            }
        }
        private static bool IsAlive(int processId)
        {
            try
            {
                // GetProcessById still finds a process that has exited while somebody holds a
                // handle to it, so it is not enough on its own. Windows also reuses the id of a
                // process that crashed, so a live process of another program is not this session
                using (Process process = Process.GetProcessById(processId))
                using (Process current = Process.GetCurrentProcess())
                {
                    if (process.HasExited) return false;
                    return string.Equals(process.ProcessName, current.ProcessName, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (Exception)
            {
                // The process cannot be inspected, which is not the same as gone
                return true;
            }
        }
        private static void RemoveStale(string fileName)
        {
            try
            {
                File.Delete(fileName);
            }
            catch (Exception)
            {
                // Another reader may be pruning the same file
            }
        }
    }
}
