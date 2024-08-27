using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using CommandLine;

namespace PrePoMax
{
    public class CommandLineOptions
    {
        // FileName
        [Option('f', "file", Required = false, HelpText = "File name to be opened/imported.")]
        public string FileName { get; set; }
        // NoGui
        [Option('n', "noGui", Required = false, HelpText = "No GUI switch hides the graphical user interface " +
                                                           "(only for regeneration).")]
        public bool NoGui { get; set; }
        // RegenerationFileName
        [Option('r', "regenerate", Required = false, HelpText = "A .pmx file name to be used for regeneration. " +
                                                                "A work directory -w must be defined for regeneration. " +
                                                                "All files needed during regeneration (geometry, mesh) " +
                                                                "must be located in the work directory.")]
        public string RegenerationFileName { get; set; }
        // UnitSystem
        [Option('u', "unitSystem", Required = false, HelpText = "Unit system type to be used when importing [M_KG_S_C | " +
                                                                "MM_TON_S_C | M_TON_S_C | IN_LB_S_F | UNIT_LESS]")]
        public string UnitSystem { get; set; }
        // WorkDirectory
        [Option('w', "workDirectory", Required = false, HelpText = "A directory path to be used as work directory.")]
        public string WorkDirectory { get; set; }


        public static string GetValuesAsString(CommandLineOptions cmdOptions)
        {
            string text = "";
            if (cmdOptions.NoGui)
                text += "No GUI: True" + Environment.NewLine;
            if (cmdOptions.FileName != null)
                text += "File name: " + cmdOptions.FileName + Environment.NewLine;
            if (cmdOptions.RegenerationFileName != null)
                text += "Regeneration file name: " + cmdOptions.RegenerationFileName + Environment.NewLine;
            if (cmdOptions.UnitSystem != null)
                text += "Unit system: " + cmdOptions.UnitSystem + Environment.NewLine;
            if (cmdOptions.WorkDirectory != null)
                text += "Work directory: " + cmdOptions.WorkDirectory + Environment.NewLine; ;
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
                // Work directory
                if (cmdOptions.WorkDirectory != null)
                {
                    if (!Directory.Exists(cmdOptions.WorkDirectory))
                        throw new CaeException("The work directory " + cmdOptions.WorkDirectory + " does not exist.");
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
                    if (!File.Exists(cmdOptions.RegenerationFileName))
                        throw new CaeException("The regeneration file " + cmdOptions.RegenerationFileName + " does not exist.");
                    if (cmdOptions.WorkDirectory == null)
                        throw new CaeException("A work directory -w must be defined for regeneration.");
                }
                else
                {
                    if (cmdOptions.NoGui == true)
                        throw new CaeException("The no GUI switch can only be used for regeneration.");
                }
            }
            catch (Exception ex)
            {
                return ex.Message + Environment.NewLine; ;
            }
            //
            return null;
        }
    }
}
