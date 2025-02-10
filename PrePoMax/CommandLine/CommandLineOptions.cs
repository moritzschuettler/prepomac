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
        [Option('g', "showGui", Required = false, Default ="Yes", HelpText = "Show Graphical User Interface [Yes | No]. " +
                                                                             "No can only be used for regeneration.")]
        public string ShowGui { get; set; }
        // Overwrite
        [Option('o', "overwrite", Required = false, Default = "No", HelpText = "Overwrite the .pmx file after regeneration " +
                                                                                "[Yes | No]. Can only be used for regeneration.")]
        public string Overwrite { get; set; }
        // Parameters
        [Option('p', "parameters", Required = false, HelpText = "Overwrite the .pmx parameters. To overwrite parameters a " +
                                                                "use [a=1.2]. To overwrite parameters a and b use quotations and " +
                                                                "semicolons [\"a=1.2; b=31.4\"]." +
                                                                "Can only be used for regeneration.")]
        public string Parameters { get; set; }
        // RegenerationFileName
        [Option('r', "regenerate", Required = false, HelpText = "A .pmx file name to be used for regeneration. " +
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
        [Option('u', "unitSystem", Required = false, HelpText = "Unit system type to be used when importing [M_KG_S_C | " +
                                                                "MM_TON_S_C | M_TON_S_C | IN_LB_S_F | UNIT_LESS].")]
        public string UnitSystem { get; set; }
        // WorkDirectory
        [Option('w', "workDirectory", Required = false, HelpText = "A directory path to be used as work directory.")]
        public string WorkDirectory { get; set; }
        // ExitAfterRegeneration
        [Option('x', "exitAfterRegeneration", Required = false, Default = "Yes", HelpText = "Exit PrePoMax after regeneration " +
                                                                                            "[Yes | No].")]
        public string ExitAfterRegeneration { get; set; }


        public static string GetValuesAsString(CommandLineOptions cmdOptions)
        {
            string text = "";
            //
            if (cmdOptions.FileName != null)
                text += "File name: " + cmdOptions.FileName + Environment.NewLine;
            if (cmdOptions.RegenerationFileName != null && cmdOptions.ShowGui != null)
                text += "Show GUI: " + cmdOptions.ShowGui + Environment.NewLine;
            if (cmdOptions.RegenerationFileName != null && cmdOptions.Overwrite != null)
                text += "Overwrite .pmx file: " + cmdOptions.Overwrite + Environment.NewLine;
            if (cmdOptions.RegenerationFileName != null && cmdOptions.Parameters != null)
                text += "Parameters: " + cmdOptions.Parameters + Environment.NewLine;
            if (cmdOptions.RegenerationFileName != null)
                text += "Regeneration file name: " + cmdOptions.RegenerationFileName + Environment.NewLine;
            if (cmdOptions.UnitSystem != null)
                text += "Unit system: " + cmdOptions.UnitSystem + Environment.NewLine;
            if (cmdOptions.WorkDirectory != null)
                text += "Work directory: " + cmdOptions.WorkDirectory + Environment.NewLine;
            if (cmdOptions.RegenerationFileName != null && cmdOptions.ExitAfterRegeneration != null)
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
                    string[] parameters = cmdOptions.Parameters.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string parameter in parameters)
                    {
                        tmp = parameter.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                        if (tmp.Length != 2 || !double.TryParse(tmp[1], out _)) error = true;
                        //
                        if (error) throw new CaeException("The parameter " + parameter + " cannot be parsed.");
                    }
                }
                // Work directory
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
                // Unit system
                if (cmdOptions.UnitSystem != null)
                {
                    if (!Enum.TryParse(cmdOptions.UnitSystem.ToUpper(), out UnitSystemType unitSystemType))
                        throw new CaeException("The unit system type " + cmdOptions.UnitSystem + " is not supported.");
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
                    if (cmdOptions.ShowGui == "No")
                        throw new CaeException("The No GUI switch can only be used for regeneration.");
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

        // Methods
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
