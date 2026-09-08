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

namespace PrePoMaxMcp
{
    // Finds the running instances. Each one writes a file named after its process id when its server
    // starts and removes it when the server stops, so a file left by a crash is recognized rather
    // than trusted: the process it names no longer exists.
    public static class SessionLocator
    {
        // Variables                                                                                                                
        private const string ApplicationFolderName = "PrePoMax";
        private const string SessionsFolderName = "sessions";


        // Static methods                                                                                                           
        public static string GetSessionsDirectory()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(Path.Combine(root, ApplicationFolderName), SessionsFolderName);
        }
        public static List<SessionInfo> GetSessions()
        {
            List<SessionInfo> sessions = new List<SessionInfo>();
            string directory = GetSessionsDirectory();
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
                SessionInfo session = Read(fileName);
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
        private static SessionInfo Read(string fileName)
        {
            try
            {
                object document;
                string error;
                if (!JsonReader.TryParse(File.ReadAllText(fileName), out document, out error)) return null;
                //
                int processId = JsonReader.GetInt(document, "processId", 0);
                string pipeName = JsonReader.GetString(document, "pipeName", null);
                if (processId <= 0 || pipeName == null) return null;
                //
                return new SessionInfo(processId, pipeName,
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
                // handle to it - the client that started it, for instance - so it is not enough
                using (Process process = Process.GetProcessById(processId))
                {
                    return !process.HasExited;
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
                // Another client may be pruning the same file
            }
        }
    }
}
