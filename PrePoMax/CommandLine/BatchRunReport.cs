// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeJob;
using PrePoMax.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace PrePoMax
{
    // Collects the machine readable record of an unattended run. A single instance is created by the
    // application entry point before the main form exists, so it is reachable from every layer that
    // must contribute to it.
    public class BatchRunReport
    {
        // Static variables
        private static BatchRunReport _current;
        // Variables
        private readonly object _syncRoot;
        private readonly DateTime _startTime;
        private readonly CommandLineOptions _options;
        private readonly string _arguments;
        private readonly List<MessageBoxPrompt> _prompts;


        // Static properties
        public static BatchRunReport Current { get { return _current; } }


        // Constructor
        private BatchRunReport(CommandLineOptions options, string arguments)
        {
            _syncRoot = new object();
            _startTime = DateTime.Now;
            _options = options;
            _arguments = arguments;
            _prompts = new List<MessageBoxPrompt>();
        }


        // Static methods
        public static void Initialize(CommandLineOptions options, string arguments)
        {
            _current = new BatchRunReport(options, arguments);
        }


        // Methods
        public bool HasRefusedPrompt()
        {
            lock (_syncRoot)
            {
                foreach (MessageBoxPrompt prompt in _prompts)
                {
                    if (prompt.IsRefused) return true;
                }
            }
            return false;
        }
        public void AddPrompt(MessageBoxPrompt prompt)
        {
            if (prompt == null) return;
            //
            lock (_syncRoot) { _prompts.Add(prompt); }
        }
        public void Write(ExitCodeEnum exitCode, Exception exception, Controller controller)
        {
            // The report must never be the reason a run fails
            try
            {
                if (_options == null || _options.ReportFileName == null) return;
                //
                JsonWriter writer = new JsonWriter();
                DateTime endTime = DateTime.Now;
                //
                writer.BeginObject();
                writer.WriteValue("version", Application.ProductVersion);
                writer.WriteValue("startTime", _startTime.ToString("o", CultureInfo.InvariantCulture));
                writer.WriteValue("endTime", endTime.ToString("o", CultureInfo.InvariantCulture));
                writer.WriteValue("durationSeconds", (endTime - _startTime).TotalSeconds);
                writer.WriteValue("exitCode", (int)exitCode);
                writer.WriteValue("exitStatus", exitCode.ToString());
                writer.WriteValue("error", exception == null ? null : exception.Message);
                //
                WriteInvocation(writer);
                WriteCommands(writer, controller);
                WriteMessages(writer);
                WritePrompts(writer);
                WriteJobs(writer, controller);
                WriteArtifacts(writer, controller);
                //
                writer.EndObject();
                //
                writer.SaveToFile(_options.ReportFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("The run report could not be written: " + ex.Message);
            }
        }
        //
        private void WriteInvocation(JsonWriter writer)
        {
            writer.BeginObject("invocation");
            writer.WriteValue("arguments", _arguments);
            writer.WriteValue("workDirectory", _options.WorkDirectory);
            writer.WriteValue("file", _options.FileName);
            writer.WriteValue("script", _options.ScriptFileName);
            writer.WriteValue("scriptArguments", _options.ScriptArguments);
            writer.WriteValue("save", _options.SaveFileName);
            writer.WriteValue("regenerationFile", _options.RegenerationFileName);
            writer.WriteValue("showGui", _options.ShowGui);
            writer.WriteValue("overwrite", _options.Overwrite);
            writer.WriteValue("onPrompt", _options.OnPrompt);
            writer.WriteValue("timeoutSeconds", _options.Timeout);
            writer.WriteValue("settingsFile", _options.SettingsFileName);
            writer.WriteValue("settingsSaved", !_options.GetIsSettingsSaveDisabled());
            writer.EndObject();
        }
        private void WriteCommands(JsonWriter writer, Controller controller)
        {
            writer.BeginArray("commands");
            //
            List<Command> commands = controller == null ? null : controller.GetCommands();
            if (commands != null)
            {
                foreach (Command command in commands)
                {
                    writer.BeginObject();
                    writer.WriteValue("name", command.Name);
                    writer.WriteValue("seconds", command.TimeSpan.TotalSeconds);
                    writer.EndObject();
                }
            }
            //
            writer.EndArray();
        }
        private void WriteMessages(JsonWriter writer)
        {
            writer.BeginArray("messages");
            //
            foreach (MessageBoxPrompt prompt in GetPrompts())
            {
                if (prompt.IsDecision) continue;
                //
                writer.BeginObject();
                writer.WriteValue("severity", GetSeverity(prompt.Caption));
                writer.WriteValue("caption", prompt.Caption);
                writer.WriteValue("text", prompt.Text);
                writer.EndObject();
            }
            //
            writer.EndArray();
        }
        private void WritePrompts(JsonWriter writer)
        {
            writer.BeginArray("prompts");
            //
            foreach (MessageBoxPrompt prompt in GetPrompts())
            {
                if (!prompt.IsDecision) continue;
                //
                writer.BeginObject();
                writer.WriteValue("caption", prompt.Caption);
                writer.WriteValue("text", prompt.Text);
                writer.WriteValue("buttons", prompt.Buttons);
                writer.WriteValue("answer", prompt.IsRefused ? null : prompt.Answer);
                writer.WriteValue("refused", prompt.IsRefused);
                writer.EndObject();
            }
            //
            writer.EndArray();
        }
        private void WriteJobs(JsonWriter writer, Controller controller)
        {
            writer.BeginArray("jobs");
            //
            if (controller != null && controller.Jobs != null)
            {
                foreach (KeyValuePair<string, AnalysisJob> entry in controller.Jobs)
                {
                    AnalysisJob job = entry.Value;
                    writer.BeginObject();
                    writer.WriteValue("name", job.Name);
                    writer.WriteValue("status", job.JobStatus.ToString());
                    writer.WriteValue("inputFile", job.InputFileName);
                    writer.WriteValue("resultFile", GetExistingResultFileName(job));
                    writer.EndObject();
                }
            }
            //
            writer.EndArray();
        }
        private void WriteArtifacts(JsonWriter writer, Controller controller)
        {
            writer.BeginArray("artifacts");
            //
            foreach (string fileName in GetArtifactFileNames(controller)) writer.WriteValue(fileName);
            //
            writer.EndArray();
        }
        //
        private List<MessageBoxPrompt> GetPrompts()
        {
            lock (_syncRoot) { return new List<MessageBoxPrompt>(_prompts); }
        }
        private List<string> GetArtifactFileNames(Controller controller)
        {
            List<string> fileNames = new List<string>();
            //
            if (_options.Overwrite == "Yes" && _options.RegenerationFileName != null)
                AddExistingFileName(fileNames, _options.RegenerationFileName);
            AddExistingFileName(fileNames, _options.ParametersExportFileName);
            //
            if (controller != null && controller.Jobs != null)
            {
                foreach (KeyValuePair<string, AnalysisJob> entry in controller.Jobs)
                {
                    AddExistingFileName(fileNames, entry.Value.InputFileName);
                    AddExistingFileName(fileNames, GetExistingResultFileName(entry.Value));
                }
            }
            //
            return fileNames;
        }
        private static void AddExistingFileName(List<string> fileNames, string fileName)
        {
            if (fileName == null || fileNames.Contains(fileName)) return;
            if (!File.Exists(fileName)) return;
            //
            fileNames.Add(fileName);
        }
        private static string GetExistingResultFileName(AnalysisJob job)
        {
            if (job.InputFileName == null) return null;
            //
            string resultFileName = Path.ChangeExtension(job.InputFileName, ".frd");
            if (resultFileName == null || !File.Exists(resultFileName)) return null;
            //
            return resultFileName;
        }
        private static string GetSeverity(string caption)
        {
            if (caption == null) return "Information";
            else if (caption.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0) return "Error";
            else if (caption.IndexOf("Warning", StringComparison.OrdinalIgnoreCase) >= 0) return "Warning";
            else return "Information";
        }
    }
}
