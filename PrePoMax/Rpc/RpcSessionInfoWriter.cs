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
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Forms;

namespace PrePoMax.Rpc
{
    // How a client finds a running instance. One file per process, named after the process id, so
    // two instances never race on it and a stale file left by a crash is recognized by the reader
    // rather than trusted: the process it names no longer exists.
    //
    // The file carries the access token of the endpoint while the server runs, so this folder's
    // permissions matter. It is per user rather than per portable copy, because a client has to find
    // every instance the user runs without knowing which folder any of them started from, and it is
    // the one thing this feature keeps outside the portable folder - transiently, since the file is
    // removed when the server stops.
    public static class RpcSessionInfoWriter
    {
        // Variables                                                                                                                
        public const string ProtocolVersion = "1.0";
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
        public static string Write(string modelFileName, DateTime startTime, string endpointUrl, int port,
                                   string accessToken)
        {
            EnsureSessionsDirectory();
            //
            Process process = Process.GetCurrentProcess();
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("processId", process.Id);
            writer.WriteValue("pipeName", GetPipeName());
            writer.WriteValue("url", endpointUrl == null ? "" : endpointUrl);
            writer.WriteValue("port", port);
            writer.WriteValue("token", accessToken == null ? "" : accessToken);
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
                // The file is a hint for list_sessions, not state the application depends on
            }
        }
        // The file holds the access token while the server runs, so the folder is created with an
        // explicit rule for this user alone and with inheritance switched off. An older version left
        // it inheriting from the profile, which is user private on an ordinary machine but need not
        // be, so a folder that already exists is protected as well rather than trusted
        private static void EnsureSessionsDirectory()
        {
            string directory = GetSessionsDirectory();
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;
            FileSystemAccessRule rule = new FileSystemAccessRule(user, FileSystemRights.FullControl,
                                                                 InheritanceFlags.ContainerInherit |
                                                                 InheritanceFlags.ObjectInherit,
                                                                 PropagationFlags.None, AccessControlType.Allow);
            if (!Directory.Exists(directory))
            {
                DirectorySecurity security = new DirectorySecurity();
                security.SetAccessRuleProtection(true, false);
                security.AddAccessRule(rule);
                Directory.CreateDirectory(directory, security);
                return;
            }
            // A folder that already works is not worth refusing to start over: the tightening is
            // reported and skipped, while a folder that cannot be created at all is fatal above
            try
            {
                DirectorySecurity existing = Directory.GetAccessControl(directory, AccessControlSections.Access);
                if (existing.AreAccessRulesProtected) return;
                //
                existing.SetAccessRuleProtection(true, false);
                existing.AddAccessRule(rule);
                Directory.SetAccessControl(directory, existing);
            }
            catch (Exception exception)
            {
                Action<string> write = MessageBoxes.WriteDataToOutput;
                if (write != null)
                {
                    write("The permissions of " + directory + " could not be restricted to the current user: " +
                          exception.Message);
                }
            }
        }
        private static string GetFileName()
        {
            string name = Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture) + ".json";
            return Path.Combine(GetSessionsDirectory(), name);
        }
    }
}
