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
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;
using PrePoMax.Python.Journal;


namespace PrePoMax.Commands
{
    [Serializable]
    class CReplaceModelProperties : PreprocessCommand, IFileDependentCommand, ICommandWithDialog
    {
        // Variables                                                                                                                
        private string _newModelName;
        private ModelProperties _newModelProperties;


        // Properties                                                                                                               
        public string FileName
        {
            get { return _newModelProperties.GlobalResultsFileName; }   // null if not a submodel
            set { if (_newModelProperties.ModelType == ModelType.Submodel) _newModelProperties.GlobalResultsFileName = value; }
        }


        // Constructor                                                                                                              
        public CReplaceModelProperties(string newModelName, ModelProperties newModelProperties)
            : base("Replace model properties")
        {
            _newModelName = newModelName;
            _newModelProperties = newModelProperties;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceModelProperties(_newModelName, _newModelProperties);
            return true;
        }
        // ICommandWithDialog
        public bool ExecuteWithDialog(Controller receiver)
        {
            if (FileName != null)
            {
                string fileName = receiver.GetFileNameToImport("Replace file: " + FileName, "Result files|*.frd");
                if (fileName != null) FileName = fileName;
            }
            return Execute(receiver);
        }

        // Every value the dialog sets is written, because a replace keeps what it is not given and a journal that
        // left out the slip wear or submodel settings replayed a different analysis
        public override void ToPython(PythonJournalWriter writer)
        {
            string globalResultsFileName = null;
            if (_newModelProperties.ModelType == ModelType.Submodel && _newModelProperties.GlobalResultsFileName != null)
            {
                string refusal;
                globalResultsFileName = GetJournalGlobalResultsFileName(writer, out refusal);
                if (refusal != null)
                {
                    writer.Unsupported(this, refusal);
                    return;
                }
            }
            List<string> arguments = new List<string>();
            // A padded or empty name is refused by the API. The dialog lets an unchanged padded name through, and
            // the command keeps no old name to tell whether it changed the name, so either is left out, which keeps
            // the name the model has; trimming a padded one would rename the model on replay
            string name = _newModelName == null ? "" : _newModelName;
            if (name.Length == 0)
                writer.Comment("The recorded model name is empty, which the API refuses, so the name is kept");
            else if (name != name.Trim())
                writer.Comment("The recorded model name '" + name + "' has leading or trailing spaces, which the API " +
                               "refuses, so it is left out and the replace keeps the name the model has");
            if (name.Length > 0 && name == name.Trim()) arguments.Add("name=" + PythonValueFormatter.Text(name));
            // The dialog changes the model space between the 2D spaces; a model nothing has set keeps Undefined
            if (_newModelProperties.ModelSpace != ModelSpaceEnum.Undefined)
                arguments.Add("model_space=" + PythonValueFormatter.Text(_newModelProperties.ModelSpace.ToString()));
            arguments.Add("model_type=" + PythonValueFormatter.Text(_newModelProperties.ModelType.ToString()));
            // The replace refuses a value the model type does not use, and keeps the hidden one it is not given
            if (globalResultsFileName != null)
                arguments.Add("global_results_file_name=" + PythonValueFormatter.Text(globalResultsFileName));
            if (_newModelProperties.ModelType == ModelType.SlipWearModel)
            {
                arguments.Add("slip_wear_results=" +
                              PythonValueFormatter.Text(_newModelProperties.SlipWearResults.ToString()));
                arguments.Add("number_of_cycles=" + PythonValueFormatter.Equation(_newModelProperties.NumberOfCycles));
                arguments.Add("cycles_increment=" + PythonValueFormatter.Equation(_newModelProperties.CyclesIncrement));
                arguments.Add("number_of_smoothing_steps=" +
                              PythonValueFormatter.Equation(_newModelProperties.NumOfSmoothingSteps));
                arguments.Add("bdm_remeshing=" + PythonValueFormatter.Format(_newModelProperties.BdmRemeshing));
            }
            // An undefined physical constant is stored as infinity, which has no Python form; the replace makes
            // every constant it is not given undefined when told to, so the replay sets exactly these constants
            bool anyConstantUndefined = false;
            anyConstantUndefined |= AddConstant(arguments, "absolute_zero", _newModelProperties.AbsoluteZero);
            anyConstantUndefined |= AddConstant(arguments, "stefan_boltzmann", _newModelProperties.StefanBoltzmann);
            anyConstantUndefined |= AddConstant(arguments, "newton_gravity", _newModelProperties.NewtonGravity);
            if (anyConstantUndefined) arguments.Add("clear_undefined_constants=True");
            writer.Statement(writer.Call("pmx.model.properties", "replace", arguments.ToArray()));
            writer.CountEmittedCommand();
        }
        // The dialog stores a bare file name, which the solver reads from the work directory; Regenerate with file
        // dialogs and a batch regeneration store a full path, which the API refuses. A path in the work directory the
        // journal is written in is written as its bare name, because a journal is replayed in a work directory of
        // its own; a path anywhere else would read another file on replay
        private string GetJournalGlobalResultsFileName(PythonJournalWriter writer, out string refusal)
        {
            refusal = null;
            string storedFileName = _newModelProperties.GlobalResultsFileName;
            string fileName;
            try
            {
                fileName = Path.GetFileName(storedFileName);
                if (Path.IsPathRooted(storedFileName))
                {
                    if (writer.WorkDirectory == null)
                        writer.Comment("The global results file '" + storedFileName + "' is written without its " +
                                       "directory, which the API does not take; the replay reads it from its own " +
                                       "work directory");
                    else if (!IsSameDirectory(Path.GetDirectoryName(storedFileName), writer.WorkDirectory))
                        refusal = "the global results file '" + storedFileName + "' is outside the work directory, " +
                                  "and the API takes only the name of a file in the work directory";
                }
                else if (fileName != storedFileName)
                {
                    refusal = "the global results file '" + storedFileName + "' has a relative path, and the API " +
                              "takes only the name of a file in the work directory";
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException ||
                                       ex is PathTooLongException)
            {
                refusal = "the global results file '" + storedFileName + "' is not a valid path: " + ex.Message;
                return null;
            }
            if (refusal == null && (fileName.Length == 0 || fileName.Contains(" ") || fileName.Contains(":") ||
                                    fileName.ToUTF8() != fileName))
                refusal = "the global results file name '" + fileName + "' is empty or contains a space, a colon or " +
                          "a special character, which the API refuses";
            return refusal == null ? fileName : null;
        }
        private static bool IsSameDirectory(string firstDirectory, string secondDirectory)
        {
            if (firstDirectory == null || secondDirectory == null) return false;
            //
            char[] separators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            string first = Path.GetFullPath(firstDirectory).TrimEnd(separators);
            string second = Path.GetFullPath(secondDirectory).TrimEnd(separators);
            return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        }
        private static bool AddConstant(List<string> arguments, string argumentName, double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value)) return true;
            //
            arguments.Add(argumentName + "=" + PythonValueFormatter.Number(value));
            return false;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _newModelName + ", " + _newModelProperties.ToString();
        }
    }
}
