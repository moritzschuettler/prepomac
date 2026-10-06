// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using CommandLine;
using PrePoMax.Commands;
using static System.Windows.Forms.Design.AxImporter;

namespace PrePoMax
{
    // https://github.com/commandlineparser/commandline                                                                             
    public class CommandLineOptions
    {
        // FileName
        [Option('f', "file", Required = false, HelpText = "File name to be opened/imported.")]
        public string FileName { get; set; }
        // Gui
        [Option('g', "showGui", Required = false, Default ="Yes", HelpText = "Show graphical user interface: Yes | No. " +
                                                                             "No can only be used for regeneration or for " +
                                                                             "running a script.")]
        public string ShowGui { get; set; }
        // Script
        [Option("script", Required = false, HelpText = "The Python script file name to be executed after the application " +
                                                       "starts. The file defined by the -f switch is opened first. " +
                                                       "The script is recorded as a single command and can be regenerated.")]
        public string ScriptFileName { get; set; }
        // Script arguments
        [Option("scriptArgs", Required = false, HelpText = "The arguments passed to the script as sys.argv. Use quotations " +
                                                           "and spaces to pass more than one argument.")]
        public string ScriptArguments { get; set; }
        // Save
        [Option("save", Required = false, HelpText = "The .pmx file name to be saved after the script finishes. The save " +
                                                     "happens after the script is recorded, so the saved file can be " +
                                                     "regenerated. Can only be used for running a script.")]
        public string SaveFileName { get; set; }
        // On prompt
        [Option("onPrompt", Required = false, HelpText = "The reaction to a dialog that asks for a decision when no user is " +
                                                         "present: Fail | Accept. Fail aborts the run, Accept answers with " +
                                                         "the default answer. The default is Fail for a script and Accept " +
                                                         "for a regeneration.")]
        public string OnPrompt { get; set; }
        // Report
        [Option("report", Required = false, HelpText = "The .json file name to be used for writing the machine readable run " +
                                                       "report. The report is written also when the run fails.")]
        public string ReportFileName { get; set; }
        // Journal
        [Option("journal", Required = false, HelpText = "The .py file name to be used for writing the session journal. The " +
                                                       "command history is emitted as a Python script when the run ends, " +
                                                       "also when the run fails.")]
        public string JournalFileName { get; set; }
        // Timeout
        [Option("timeout", Required = false, Default = 0, HelpText = "The maximum duration of the run in seconds. Use 0 for no " +
                                                                     "limit. On expiry the running analyses are killed and the " +
                                                                     "application exits.")]
        public int Timeout { get; set; }
        // Automation server
        [Option("rpc", Required = false, Default = "Off", HelpText = "Start the automation server so an AI agent can drive " +
                                                                     "this session: On | ReadOnly | Off. ReadOnly refuses " +
                                                                     "every call that would change the model. The MCP " +
                                                                     "endpoint listens on this computer only and needs the " +
                                                                     "access token of the session.")]
        public string Rpc { get; set; }
        // Automation server port
        [Option("rpcPort", Required = false, Default = 0, HelpText = "The port the MCP endpoint listens on, instead of the " +
                                                                      "one in the settings. When it is taken the endpoint " +
                                                                      "takes the first free port above it.")]
        public int RpcPort { get; set; }
        // Automation server request timeout
        [Option("rpcTimeout", Required = false, Default = 0, HelpText = "How many seconds the automation server waits for " +
                                                                         "a request, instead of the request timeout in the " +
                                                                         "settings. A Python call that takes longer keeps " +
                                                                         "running and is answered with a call id to collect " +
                                                                         "its result by. This is not --timeout, which limits " +
                                                                         "the whole run.")]
        public int RpcTimeout { get; set; }
        // Settings
        [Option("settings", Required = false, HelpText = "The settings file name to be used instead of the settings file " +
                                                          "located next to the executable.")]
        public string SettingsFileName { get; set; }
        // No settings save
        [Option("noSettingsSave", Required = false, Default = false, HelpText = "Do not write the settings file. This is " +
                                                                                 "always used when a script is executed.")]
        public bool NoSettingsSave { get; set; }
        // Overwrite
        [Option('o', "overwrite", Required = false, Default = "No", HelpText = "Overwrite the .pmx file after regeneration: " +
                                                                               "Yes | No. Can only be used for regeneration.")]
        public string Overwrite { get; set; }
        // Parameters
        [Option('p', "parameters", Required = false, HelpText = "Overwrite the model parameters. To overwrite parameter p1 " +
                                                                "use p1=1.2. To overwrite parameters p1 and p2 use quotations " +
                                                                "and semicolons \"a=1.2; b=31.4\". To overwrite parameters by " +
                                                                "importing a parameters file, specify the .pmp file name." +
                                                                "Can only be used for regeneration.")]
        public string Parameters { get; set; }
        // Parameters export
        [Option("parametersExport", Required = false, HelpText = "The .pmp file name to be used for exporting parameters from the " +
                                                                 ".pmx file defined by the -r switch.")]
        public string ParametersExportFileName { get; set; }
        // RegenerationFileName
        [Option('r', "regenerate", Required = false, HelpText = "The .pmx file name to be used for regeneration. " +
                                                                "Use r1, r2 or r3 to only regenerate the pre-processing commands, " +
                                                                "the analysis commands or the post-processing commands." +
                                                                "A work directory -w can be defined for regeneration. " +
                                                                "If no work directory is defined the current directory is used. " +
                                                                "All files needed during regeneration (geometry, mesh) " +
                                                                "must be located in the work directory.")]
        public string RegenerationFileName
        {
            get
            {
                if (Regeneration0FileName != null) return Regeneration0FileName;
                else if (Regeneration1FileName != null) return Regeneration1FileName;
                else if (Regeneration2FileName != null) return Regeneration2FileName;
                else if (Regeneration3FileName != null) return Regeneration3FileName;
                else return null;
            }
            set
            {
                Regeneration0FileName = value;
            }
        }
        // RegenerationFileName
        [Option("r0", HelpText = "Regeneration of all history commands. See option -r.")]
        public string Regeneration0FileName { get; set; }
        [Option("r1", HelpText = "Regeneration of pre-processing history commands only.")]
        public string Regeneration1FileName { get; set; }
        [Option("r2", HelpText = "Regeneration of analysis history commands only.")]
        public string Regeneration2FileName { get; set; }
        [Option("r3", HelpText = "Regeneration of post-processing history commands only.")]
        public string Regeneration3FileName { get; set; }


        // UnitSystem
        [Option('u', "unitSystem", Required = false, HelpText = "Unit system type to be used when importing: M_KG_S_C | " +
                                                                "MM_TON_S_C | M_TON_S_C | IN_LB_S_F | UNIT_LESS.")]
        public string UnitSystem { get; set; }
        // WorkDirectory
        [Option('w', "workDirectory", Required = false, HelpText = "The directory path to be used as work directory.")]
        public string WorkDirectory { get; set; }
        // ExitAfterRegeneration
        [Option('x', "exitAfterRegeneration", Required = false, Default = "Yes", HelpText = "Exit PrePoMax after regeneration: " +
                                                                                            "Yes | No.")]
        public string ExitAfterRegeneration { get; set; }


        public static string GetValuesAsString(CommandLineOptions cmdOptions)
        {
            string text = "";
            //
            if (cmdOptions.FileName != null)
                text += "File name: " + cmdOptions.FileName + Environment.NewLine;
            if (cmdOptions.GetIsBatchMode() && cmdOptions.ShowGui != null)
                text += "Show GUI: " + cmdOptions.ShowGui + Environment.NewLine;
            if (cmdOptions.ScriptFileName != null)
                text += "Script file name: " + cmdOptions.ScriptFileName + Environment.NewLine;
            if (cmdOptions.ScriptArguments != null)
                text += "Script arguments: " + cmdOptions.ScriptArguments + Environment.NewLine;
            if (cmdOptions.GetIsBatchMode() && cmdOptions.OnPrompt != null)
                text += "On prompt: " + cmdOptions.OnPrompt + Environment.NewLine;
            if (cmdOptions.ReportFileName != null)
                text += "Report file name: " + cmdOptions.ReportFileName + Environment.NewLine;
            if (cmdOptions.Timeout > 0)
                text += "Timeout: " + cmdOptions.Timeout + " s" + Environment.NewLine;
            if (cmdOptions.SettingsFileName != null)
                text += "Settings file name: " + cmdOptions.SettingsFileName + Environment.NewLine;
            if (cmdOptions.RegenerationFileName != null && cmdOptions.Overwrite != null)
                text += "Overwrite .pmx file: " + cmdOptions.Overwrite + Environment.NewLine;
            if (cmdOptions.RegenerationFileName != null && cmdOptions.Parameters != null)
                text += "Parameters: " + cmdOptions.Parameters + Environment.NewLine;
            if (cmdOptions.RegenerationFileName != null && cmdOptions.ParametersExportFileName != null)
                text += "Export parameters file name: " + cmdOptions.ParametersExportFileName + Environment.NewLine;
            if (cmdOptions.RegenerationFileName != null)
                text += "Regeneration file name: " + cmdOptions.RegenerationFileName + Environment.NewLine;
            if (cmdOptions.UnitSystem != null)
                text += "Unit system: " + cmdOptions.UnitSystem + Environment.NewLine;
            if (cmdOptions.WorkDirectory != null)
                text += "Work directory: " + cmdOptions.WorkDirectory + Environment.NewLine;
            if (cmdOptions.GetIsBatchMode() && cmdOptions.ExitAfterRegeneration != null)
                text += "Exit after regeneration : " + cmdOptions.ExitAfterRegeneration + Environment.NewLine;
            //
            if (text.Length > 0) text = "----------Parameters----------" + Environment.NewLine + text;
            else text = null;
            //
            return text;
        }
        public static string CheckForErrors(CommandLineOptions cmdOptions)
        {
            try
            {
                // Work directory - should be first
                if (cmdOptions.WorkDirectory != null)
                {
                    if (!Directory.Exists(cmdOptions.WorkDirectory))
                        throw new CaeException("The work directory " + cmdOptions.WorkDirectory + " does not exist.");
                }
                else
                {
                    // Use current directory if no work directory is specified
                    cmdOptions.WorkDirectory = Directory.GetCurrentDirectory();
                }
                // Options
                if (cmdOptions == null)
                    throw new CaeException("The command line parameters are null.");
                // FileName
                if (cmdOptions.FileName != null)
                {
                    if (!File.Exists(cmdOptions.FileName))
                        throw new Exception("The file " + cmdOptions.FileName + " does not exist.");
                }
                // Gui
                string gui = cmdOptions.ShowGui.ToUpper().Trim();
                if (gui == "YES") cmdOptions.ShowGui = "Yes";        // fix all caps and spaces
                else if (gui == "NO") cmdOptions.ShowGui = "No";     // fix all caps and spaces
                else throw new CaeException("Show GUI switch can only be set to Yes or No.");
                // Overwrite
                string overwrite = cmdOptions.Overwrite.ToUpper().Trim();
                if (overwrite == "YES") cmdOptions.Overwrite = "Yes";        // fix all caps and spaces
                else if (overwrite == "NO") cmdOptions.Overwrite = "No";     // fix all caps and spaces
                else throw new CaeException("Overwrite switch can only be set to Yes or No.");
                // Parameters
                bool error = false;
                string[] tmp;
                if (cmdOptions.Parameters != null)
                {
                    if (cmdOptions.Parameters.Contains("="))
                    {
                        string[] parameters = cmdOptions.Parameters.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string parameter in parameters)
                        {
                            tmp = parameter.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                            if (tmp.Length != 2 || !double.TryParse(tmp[1], out _)) error = true;
                            //
                            if (error) throw new CaeException("The parameter " + parameter + " cannot be parsed.");
                        }
                    }
                    else // use parameters file
                    {
                        string fileDirectory = Path.GetDirectoryName(cmdOptions.Parameters);
                        string fileName = Path.GetFileName(cmdOptions.Parameters);
                        if (fileDirectory == "")
                        {
                            fileName = Path.Combine(cmdOptions.WorkDirectory, fileName);
                            cmdOptions.Parameters = fileName;
                        }
                        //
                        if (!File.Exists(cmdOptions.Parameters))
                        {
                            throw new CaeException("The parameters file " + cmdOptions.Parameters + " does not exist.");
                        }
                    }
                }
                // Parameters export
                if (cmdOptions.ParametersExportFileName != null)
                {
                    string fileDirectory = Path.GetDirectoryName(cmdOptions.ParametersExportFileName);
                    string fileName = Path.GetFileName(cmdOptions.ParametersExportFileName);
                    if (fileDirectory == "")
                    {
                        fileName = Path.Combine(cmdOptions.WorkDirectory, fileName);
                        cmdOptions.ParametersExportFileName = fileName;
                    }
                }
                // Regeneration
                if (cmdOptions.RegenerationFileName != null)
                {
                    string fileDirectory = Path.GetDirectoryName(cmdOptions.RegenerationFileName);
                    string fileName = Path.GetFileName(cmdOptions.RegenerationFileName);
                    if (fileDirectory == "")
                    {
                        fileName = Path.Combine(cmdOptions.WorkDirectory, fileName);
                        if (cmdOptions.Regeneration0FileName != null) cmdOptions.Regeneration0FileName = fileName;
                        else if (cmdOptions.Regeneration1FileName != null) cmdOptions.Regeneration1FileName = fileName;
                        else if (cmdOptions.Regeneration2FileName != null) cmdOptions.Regeneration2FileName = fileName;
                        else if (cmdOptions.Regeneration3FileName != null) cmdOptions.Regeneration3FileName = fileName;
                        else throw new NotSupportedException();
                    }
                    //
                    if (!File.Exists(cmdOptions.RegenerationFileName))
                    {
                        throw new CaeException("The regeneration file " + cmdOptions.RegenerationFileName + " does not exist.");
                    }
                }
                else
                {
                    if (cmdOptions.Overwrite == "Yes")
                        throw new CaeException("The overwrite switch can only be used for regeneration.");
                    if (cmdOptions.Parameters != null)
                        throw new CaeException("The parameters switch can only be used for regeneration.");
                    if (cmdOptions.ShowGui == "No" && cmdOptions.ScriptFileName == null && cmdOptions.Rpc == "Off")
                    {
                        throw new CaeException("The No GUI switch can only be used for regeneration, for running a script " +
                                               "or with the automation server.");
                    }
                }
                // Script
                if (cmdOptions.ScriptFileName != null)
                {
                    if (cmdOptions.RegenerationFileName != null)
                        throw new CaeException("The script switch cannot be combined with regeneration.");
                    //
                    cmdOptions.ScriptFileName = GetFullFileName(cmdOptions.ScriptFileName, cmdOptions.WorkDirectory);
                    //
                    if (!File.Exists(cmdOptions.ScriptFileName))
                        throw new CaeException("The script file " + cmdOptions.ScriptFileName + " does not exist.");
                }
                else
                {
                    if (cmdOptions.ScriptArguments != null)
                        throw new CaeException("The script arguments switch can only be used for running a script.");
                    if (cmdOptions.SaveFileName != null)
                        throw new CaeException("The save switch can only be used for running a script.");
                }
                // Save
                if (cmdOptions.SaveFileName != null)
                {
                    cmdOptions.SaveFileName = GetFullFileName(cmdOptions.SaveFileName, cmdOptions.WorkDirectory);
                    //
                    string saveDirectory = Path.GetDirectoryName(cmdOptions.SaveFileName);
                    if (saveDirectory != null && saveDirectory.Length > 0 && !Directory.Exists(saveDirectory))
                        throw new CaeException("The save directory " + saveDirectory + " does not exist.");
                }
                // On prompt
                if (cmdOptions.OnPrompt != null)
                {
                    string onPrompt = cmdOptions.OnPrompt.ToUpper().Trim();
                    if (onPrompt == "FAIL") cmdOptions.OnPrompt = "Fail";           // fix all caps and spaces
                    else if (onPrompt == "ACCEPT") cmdOptions.OnPrompt = "Accept";  // fix all caps and spaces
                    else throw new CaeException("On prompt switch can only be set to Fail or Accept.");
                }
                else
                {
                    // A script must not have decisions taken for it; regeneration keeps the established behaviour
                    cmdOptions.OnPrompt = cmdOptions.ScriptFileName != null ? "Fail" : "Accept";
                }
                // Report
                if (cmdOptions.ReportFileName != null)
                {
                    cmdOptions.ReportFileName = GetFullFileName(cmdOptions.ReportFileName, cmdOptions.WorkDirectory);
                    //
                    string reportDirectory = Path.GetDirectoryName(cmdOptions.ReportFileName);
                    if (reportDirectory != null && reportDirectory.Length > 0 && !Directory.Exists(reportDirectory))
                        throw new CaeException("The report directory " + reportDirectory + " does not exist.");
                }
                // Automation server
                string rpc = cmdOptions.Rpc == null ? "OFF" : cmdOptions.Rpc.ToUpper().Trim();
                if (rpc == "ON") cmdOptions.Rpc = "On";                    // fix all caps and spaces
                else if (rpc == "READONLY") cmdOptions.Rpc = "ReadOnly";   // fix all caps and spaces
                else if (rpc == "OFF") cmdOptions.Rpc = "Off";             // fix all caps and spaces
                else throw new CaeException("The rpc switch can only be set to On, ReadOnly or Off.");
                // Zero is what says the settings decide, so it is not out of range
                if (cmdOptions.RpcPort != 0 &&
                    (cmdOptions.RpcPort < AutomationSettings.MinPort || cmdOptions.RpcPort > AutomationSettings.MaxPort))
                {
                    throw new CaeException("The rpcPort switch must be between " + AutomationSettings.MinPort +
                                           " and " + AutomationSettings.MaxPort + ".");
                }
                if (cmdOptions.RpcTimeout != 0 &&
                    (cmdOptions.RpcTimeout < AutomationSettings.MinRequestTimeout ||
                     cmdOptions.RpcTimeout > AutomationSettings.MaxRequestTimeout))
                {
                    throw new CaeException("The rpcTimeout switch must be between " + AutomationSettings.MinRequestTimeout +
                                           " and " + AutomationSettings.MaxRequestTimeout + " seconds.");
                }
                // Timeout
                if (cmdOptions.Timeout < 0)
                    throw new CaeException("The timeout switch must not be negative.");
                // Settings
                if (cmdOptions.SettingsFileName != null)
                {
                    cmdOptions.SettingsFileName = GetFullFileName(cmdOptions.SettingsFileName, cmdOptions.WorkDirectory);
                    //
                    if (!File.Exists(cmdOptions.SettingsFileName))
                        throw new CaeException("The settings file " + cmdOptions.SettingsFileName + " does not exist.");
                }
                // Unit system
                if (cmdOptions.UnitSystem != null)
                {
                    if (!Enum.TryParse(cmdOptions.UnitSystem.ToUpper(), out UnitSystemType unitSystemType))
                        throw new CaeException("The unit system type " + cmdOptions.UnitSystem + " is not supported.");
                }
                // Exit
                string exit = cmdOptions.ExitAfterRegeneration.ToUpper().Trim();
                if (exit == "YES") cmdOptions.ExitAfterRegeneration = "Yes";        // fix all caps and spaces
                else if (exit == "NO") cmdOptions.ExitAfterRegeneration = "No";     // fix all caps and spaces
                else throw new CaeException("Exit switch can only be set to Yes or No.");
            }
            catch (Exception ex)
            {
                return ex.Message + Environment.NewLine; ;
            }
            //
            return null;
        }

        // Static methods
        private static string GetFullFileName(string fileName, string workDirectory)
        {
            string fileDirectory = Path.GetDirectoryName(fileName);
            if (fileDirectory == "") return Path.Combine(workDirectory, Path.GetFileName(fileName));
            else return fileName;
        }


        // Methods
        public bool GetIsBatchMode()
        {
            return RegenerationFileName != null || ScriptFileName != null;
        }
        public bool GetIsSettingsSaveDisabled()
        {
            // A script run must never change the settings of the interactive user
            return NoSettingsSave || ScriptFileName != null;
        }
        public MessageBoxModeEnum GetMessageBoxMode()
        {
            if (ShowGui != "No") return MessageBoxModeEnum.Interactive;
            else if (OnPrompt == "Fail") return MessageBoxModeEnum.Strict;
            else return MessageBoxModeEnum.AutoAnswer;
        }
        public RegenerateTypeEnum GetRegenerateType()
        {
            RegenerateTypeEnum regenerateType;
            if (Regeneration0FileName != null) regenerateType = RegenerateTypeEnum.All;
            else if (Regeneration1FileName != null) regenerateType = RegenerateTypeEnum.PreProcess;
            else if (Regeneration2FileName != null) regenerateType = RegenerateTypeEnum.Analysis;
            else if (Regeneration3FileName != null) regenerateType = RegenerateTypeEnum.PostProcess;
            else throw new NotSupportedException();
            return regenerateType;
        }
    }
}
