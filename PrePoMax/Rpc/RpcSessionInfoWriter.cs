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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace PrePoMax.Rpc
{
    // How the bridge finds a running instance. One file per process, named after the process id, so
    // two instances never race on it and a stale file left by a crash is recognized by the reader
    // rather than trusted: the process it names no longer exists.
    public static class RpcSessionInfoWriter
    {
        // Variables                                                                                                                
        public const string ProtocolVersion = "1.0";
        // Globals.ProgramName carries the version, and the bridge has to find the folder without
        // knowing which version wrote it
        private const string ApplicationFolderName = "PrePoMax";
        private const string SessionsFolderName = "sessions";


        // Static methods                                                                                                           
        public static string GetSessionsDirectory()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(Path.Combine(root, ApplicationFolderName), SessionsFolderName);
        }
        public static string GetPipeName()
        {
            return "PrePoMax." + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture);
        }
        public static string Write(string modelFileName, DateTime startTime)
        {
            string directory = GetSessionsDirectory();
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            //
            Process process = Process.GetCurrentProcess();
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("processId", process.Id);
            writer.WriteValue("pipeName", GetPipeName());
            writer.WriteValue("protocolVersion", ProtocolVersion);
            writer.WriteValue("applicationVersion", Application.ProductVersion);
            writer.WriteValue("modelFileName", modelFileName == null ? "" : modelFileName);
            writer.WriteValue("startTime", startTime.ToString("s", CultureInfo.InvariantCulture));
            writer.EndObject();
            //
            string fileName = GetFileName();
            writer.SaveToFile(fileName);
            return fileName;
        }
        public static void Remove()
        {
            try
            {
                string fileName = GetFileName();
                if (File.Exists(fileName)) File.Delete(fileName);
            }
            catch
            {
                // The file is a hint for the bridge, not state the application depends on
            }
        }
        private static string GetFileName()
        {
            string name = Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture) + ".json";
            return Path.Combine(GetSessionsDirectory(), name);
        }
    }
}
