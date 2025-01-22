using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CaeGlobals;
using FastColoredTextBoxNS;
using CommandLine;
using System.Diagnostics;
using System.Windows.Forms;

namespace PrePoMax.Commands
{
    [Serializable]
    public enum RegenerateTypeEnum
    {
        All,
        PreProcess,
        Analysis,
        PostProcess
    }

    [Serializable]
    public class CommandsCollection
    {
        
        // Variables                                                                                                                
        private int _currPositionIndex;
        private Controller _controller;
        private List<Command> _commands;
        private string _historyFileNameBin;
        private ViewGeometryModelResults _previousView;
        private List<string> _errors;


        // Properties                                                                                                               
        public int Count { get { return _commands.Count(); } }
        public int CurrPositionIndex { get { return _currPositionIndex; } }
        public List<Command> Commands { get { return _commands; } }
        public bool IsEnableDisableUndoRedoDefined { get { return EnableDisableUndoRedo != null; } }
        public List<string> Errors { get { return _errors; } }


        // Callbacks                                                                                                                
        [NonSerialized] public Action<string> WriteOutput;


        // Events                                                                                                                   
        public event Action<string, string> EnableDisableUndoRedo;


        // Constructor                                                                                                              
        public CommandsCollection(Controller controller)
        {
            _controller = controller;
            _currPositionIndex = -1;
            _commands = new List<Command>();
            _historyFileNameBin = Path.Combine(System.Windows.Forms.Application.StartupPath, Globals.HistoryFileName);
            _previousView = ViewGeometryModelResults.Geometry;
            _errors = null;
            //
            WriteToFile();
        }
        public CommandsCollection(Controller controller, CommandsCollection commandsCollection)
            :this(controller)
        {
            _currPositionIndex = commandsCollection._currPositionIndex;
            _commands = commandsCollection._commands;
            _previousView = commandsCollection._previousView;
            //
            WriteToFile();
        }


        // Methods                                                                                                                  
        public bool AddAndExecute(Command command)
        {
            return ExecuteCommand(command, true);
        }
        public bool AddAndExecute(Command command, bool executeSynchronous)
        {
            return ExecuteCommand(command, true, executeSynchronous);
        }
        public bool AddAndExecute(List<Command> commands)
        {
            bool result = true;
            _errors = new List<string>();
            //
            if (commands != null && commands.Count > 0)
            {
                foreach (var command in commands)
                {
                    try
                    {
                        if (command is CSaveToPmx) { }  // skip save commands
                        else if (command is CClear)
                        {
                            Clear();    // remove all previous commands
                            result &= AddAndExecute(command, true);
                        }
                        else result &= AddAndExecute(command, true);
                    }
                    catch (Exception ex)
                    {
                        result = false;
                        _errors.Add(command.Name + ": " + ex.Message);
                    }
                }
                // Report Errors
                if (_errors.Count != 0)
                {
                    WriteOutput?.Invoke("");
                    WriteOutput?.Invoke("****   Exceptions   ****");
                    foreach (var error in _errors)
                    {
                        WriteOutput?.Invoke(error);
                    }
                    WriteOutput?.Invoke("****   Number of exceptions: " + _errors.Count + "   ****");
                }
            }
            //
            return result;
        }
        private void AddCommand(Command command)
        {
            // Remove old commands
            if (_currPositionIndex < _commands.Count - 1)
                _commands.RemoveRange(_currPositionIndex + 1, _commands.Count - _currPositionIndex - 1);
            //
            _commands.Add(command);
        }
        private void CheckModelChanged(Command command)
        {
            if (command is CClear) return;
            else if (command is SaveCommand) { }
            else _controller.ModelChanged = true;
        }
        public void SetCommands(List<Command> commands)
        {
            _currPositionIndex = -1;
            _commands.Clear();
            //
            foreach (Command command in commands)
            {
                // Add command
                AddCommand(command);
                //
                _currPositionIndex++;
            }
            // Write to file
            WriteToFile();
            //
            OnEnableDisableUndoRedo();
            // Model changed
            _controller.ModelChanged = true;
        }
        private bool ExecuteCommand(Command command, bool addCommand, bool executeSynchronous = false)
        {
            bool result = false;
            // Write to form
            WriteToOutput(command);
            // First execute to check for errors
            if (command is SaveCommand || ExecuteCommandWithTimer(command, false, executeSynchronous))
            {
                // Add command
                if (addCommand) AddCommand(command);
                // Check model changed
                CheckModelChanged(command);
                // Write to file
                WriteToFile();
                //
                _currPositionIndex++;
                //
                OnEnableDisableUndoRedo();
                //
                result = true;
            }
            // Execute the save command at the end to include all changes in the file
            if (command is SaveCommand)
            {
                ExecuteCommandWithTimer(command, false, executeSynchronous);
                WriteToFile();  // repeat the write in order to save the hash
            }
            //
            return result;
        }
        private bool ExecuteCommandWithTimer(Command command, bool executeWithDialog = false, bool executeSynchronous = false)
        {
            bool result;
            Stopwatch stopwatch = Stopwatch.StartNew();
            //
            if (executeWithDialog && command is ICommandWithDialog cwd)
            {
                result = cwd.ExecuteWithDialog(_controller);
            }
            else
            {
                // Execute asynchronous tasks in synchronous mode
                if (executeSynchronous && command is ICommandAsynchronous ca) result = ca.ExecuteSynchronous(_controller);
                else result = command.Execute(_controller);
            }
            //
            stopwatch.Stop();
            command.TimeSpan = stopwatch.Elapsed;
            //
            return result;
        }
        public void ExecuteAllCommands()
        {
            ExecuteAllCommands(false, false, RegenerateTypeEnum.All, null);
        }
        public void ExecuteAllCommands(bool showFileDialog, bool showMeshDialog, RegenerateTypeEnum regenerateType)
        {
            ExecuteAllCommands(showFileDialog, showMeshDialog, regenerateType, null);
        }
        public void ExecuteAllCommandsFromLastSave(RegenerateTypeEnum regenerateType, SaveCommand lastSave)
        {
            ExecuteAllCommands(false, false, regenerateType, lastSave);
        }
        public void ExecuteAllCommands(bool showFileDialog, bool showMeshDialog, RegenerateTypeEnum regenerateType,
                                       SaveCommand lastSave)
        {
            int count = 0;
            bool executeWithDialog;
            _errors = new List<string>();
            //
            foreach (Command command in _commands)    // clear command modifies the collection
            {
                if (count++ <= _currPositionIndex)
                {
                    // Set working directory while in regeneration mode
                    if (_controller.BatchRegenerationMode)
                    {
                        if (command is COpenFile cOpenFile)
                        {
                            string newFileName = Path.Combine(_controller.Settings.GetWorkDirectory(),
                                                              Path.GetFileName(cOpenFile.FileName));
                            if (File.Exists(newFileName)) cOpenFile.FileName = newFileName;
                        }
                        else if (command is CImportFile cImportFile)
                        {
                            string newFileName = Path.Combine(_controller.Settings.GetWorkDirectory(),
                                                              Path.GetFileName(cImportFile.FileName));
                            if (File.Exists(newFileName)) cImportFile.FileName = newFileName;
                        }
                        else if (command is CExportResultHistoryOutput cExportResultHistoryOutput)
                        {
                            string newFileName = Path.Combine(_controller.Settings.GetWorkDirectory(),
                                                              Path.GetFileName(cExportResultHistoryOutput.FileName));
                            cExportResultHistoryOutput.FileName = newFileName;
                        }
                    }
                    // Skip save before writing to form - set lastSave to null
                    if (command is SaveCommand)
                    {
                        if (lastSave != null && command == lastSave) lastSave = null;
                        continue;
                    }
                    // Skip command before writing to form
                    if ((regenerateType == RegenerateTypeEnum.PreProcess && !(command is PreprocessCommand)) ||
                        (regenerateType == RegenerateTypeEnum.Analysis && !(command is AnalysisCommand)) ||
                        (regenerateType == RegenerateTypeEnum.PostProcess && !(command is PostprocessCommand)))
                        continue;
                    // Try
                    try
                    {
                        // Skip all up to last save
                        if (lastSave != null) { }
                        // Execute
                        else
                        {
                            // Write to form
                            WriteToOutput(command);
                            //
                            executeWithDialog = command is ICommandWithDialog cwd && 
                                                (showFileDialog && cwd is COpenFile ||
                                                 showFileDialog && cwd is CImportFile ||
                                                 showMeshDialog && cwd is CAddMeshSetupItem ||
                                                 showMeshDialog && cwd is CReplaceMeshSetupItem);
                            //
                            ExecuteCommandWithTimer(command, executeWithDialog, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        _errors.Add(command.Name + ": " + ex.Message);
                    }
                    // Check model changed
                    CheckModelChanged(command);
                }
                else break;
            }
            // Report Errors
            if (_errors.Count != 0)
            {
                WriteOutput?.Invoke("");
                WriteOutput?.Invoke("****   Exceptions   ****");                
                foreach (var error in _errors)
                {
                    WriteOutput?.Invoke(error);
                }
                WriteOutput?.Invoke("****   Number of exceptions: " + _errors.Count + "   ****");
            }
            // Write to file
            WriteToFile();
            //
            OnEnableDisableUndoRedo();
        }
        public CSaveToPmx GetLastSaveCommand()
        {
            Command[] reversed = _commands.ToArray().Reverse().ToArray();   // must be like this
            //
            for (int i = 0; i < reversed.Length; i++)
            {
                if (reversed[i] is CSaveToPmx cstp && cstp.IsFileHashUnchanged()) return cstp;
            }
            //
            return null;
        }
        public Command GetLastExecutedCommand(RegenerateTypeEnum regenerateType)
        {
            Command lastCommand = null;
            foreach (var command in _commands)
            {
                if ((regenerateType == RegenerateTypeEnum.PreProcess && !(command is PreprocessCommand)) ||
                    (regenerateType == RegenerateTypeEnum.Analysis && !(command is AnalysisCommand)) ||
                    (regenerateType == RegenerateTypeEnum.PostProcess && !(command is PostprocessCommand)) ||
                    command is SaveCommand)
                    continue;
                //
                lastCommand = command;
            }
            return lastCommand;
        }
        // Set execution time for async commands
        public void SetLastAnalysisTime(TimeSpan timeSpan)
        {
            Command[] reversed = _commands.ToArray().Reverse().ToArray();   // must be like this
            //
            for (int i = 0; i < reversed.Length; i++)
            {
                if (reversed[i] is CPrepareAndRunJob cprj)
                {
                    cprj.TimeSpan = timeSpan;
                    break;
                }
            }
        }
        public void SetLastOpenResultsTime(TimeSpan timeSpan)
        {
            Command[] reversed = _commands.ToArray().Reverse().ToArray();   // must be like this
            //
            for (int i = 0; i < reversed.Length; i++)
            {
                if (reversed[i] is COpenResults cor)
                {
                    cor.TimeSpan = timeSpan;
                    break;
                }
            }
        }
        // Post-process
        public bool RunHistoryPostprocessing()
        {
            string currentAnalysisName = null;
            string lastAnalysisName = null;
            List<Command> commandsList;
            Dictionary<string, List<Command>> runCommands = new Dictionary<string, List<Command>>();
            Dictionary<string, List<Command>> postprocessCommands = new Dictionary<string, List<Command>>();
            // Collect pos-processing commands by analysis name
            foreach (var command in _commands)
            {
                // Set current analysis
                if (command is COpenResults or)
                {
                    currentAnalysisName = or.JobName;
                    lastAnalysisName = or.JobName;
                }
                else if (command is CSetCurrentResults scr)
                {
                    currentAnalysisName = scr.JobName;
                }
                //                                                                          
                // Add all run analysis commands to dictionary
                if (command is CPrepareAndRunJob prj)
                {
                    if (runCommands.TryGetValue(prj.JobName, out commandsList)) commandsList.Add(command);
                    else runCommands.Add(prj.JobName, new List<Command> { command });
                }
                // Add all post processing commands to dictionary
                else if (command is PostprocessCommand)
                {
                    if (postprocessCommands.TryGetValue(currentAnalysisName, out commandsList)) commandsList.Add(command);
                    else postprocessCommands.Add(currentAnalysisName, new List<Command> { command });
                }
            }
            // Run postprocessing commands for the last opened analysis
            List<Command> runCommandsList;
            List<Command> postprocessCommandsList;
            if (lastAnalysisName != null && postprocessCommands.TryGetValue(lastAnalysisName, out postprocessCommandsList) &&
                runCommands.TryGetValue(lastAnalysisName, out runCommandsList))
            {
                int count = 0;
                COpenResults lastOpenResultsCommand = null;
                List<Command> reducedCommands = new List<Command>();
                // Remove unnecessary run commands                                          
                foreach (var command in runCommandsList)
                {
                    if (count++ < runCommandsList.Count - 1) _commands.Remove(command);
                }
                // Find last open results command
                foreach (var command in postprocessCommandsList)
                {
                    if (command is COpenResults or) lastOpenResultsCommand = or;
                }
                // Remove commands
                foreach (var command in postprocessCommandsList)
                {
                    if (command != lastOpenResultsCommand) _commands.Remove(command);
                }
                // Remove unnecessary commands                                              
                foreach (var command in postprocessCommandsList)
                {
                    if (command is COpenResults) { }
                    if (command is COpenResults) { }
                    else if (command is CSetCurrentResults) { }
                    else reducedCommands.Add(command);
                }
                // Run commands
                return AddAndExecute(reducedCommands);
            }
            //
            return true;
        }

        // Clear
        public void Clear()
        {
            _currPositionIndex = -1;
            _commands.Clear();
            _previousView = ViewGeometryModelResults.Geometry;
            _errors = null;
            // Write to file
            WriteToFile();
            //
            OnEnableDisableUndoRedo();
        }
        //
        public void SaveToSeparateFiles(string folderName)
        {
            int i = 1;
            string fileName;
            foreach (var command in _commands)
            {
                if (i == 934)
                    i = 934;
                fileName = Path.Combine(folderName, i++.ToString().PadLeft(4, '0') + "_" + command.Name.Replace("/", "") + ".cmd");
                command.DumpToFile(fileName);
            }
        }
        // Undo / Redo
        public void Undo(RegenerateTypeEnum regenerateType)
        {
            if (IsUndoPossible)
            {
                _currPositionIndex--;
                ExecuteAllCommands(false, false, regenerateType);   // also rewrites history
                //
                OnEnableDisableUndoRedo();
            }
        }
        public void Redo()
        {
            if (IsRedoPossible)
            {
                //_currPositionIndex++;
                ExecuteCommand(_commands[_currPositionIndex + 1], false);  // also rewrites history
            }
        }
        public void OnEnableDisableUndoRedo()
        {
            string undo = null;
            string redo = null;
            //
            if (_currPositionIndex >= _commands.Count) _currPositionIndex = _commands.Count - 1;
            //
            if (IsUndoPossible) undo = _commands[_currPositionIndex].Name;
            if (IsRedoPossible) redo = _commands[_currPositionIndex + 1].Name;
            //
            if (EnableDisableUndoRedo != null) EnableDisableUndoRedo(undo, redo);
        }
        private bool IsUndoPossible
        {
            get { return _currPositionIndex > -1; }
        }
        private bool IsRedoPossible
        {
            get { return _currPositionIndex < _commands.Count - 1; }
        }
        // Write
        private void WriteToOutput(Command command)
        {
            if (command is CClear) return;
            string data = command.GetCommandString();
            if (data.Length > 20) data = data.Substring(20);    // Remove date and time for the write to form
            WriteOutput?.Invoke(data);
        }
        private void WriteToFile()
        {
            if (!_controller.BatchRegenerationMode && _commands.Count > 1)
            {
                // Use other file
                WriteToFile(_commands, _historyFileNameBin);
            }
        }
        public static void WriteToFile(List<Command> commands, string fileName)
        {
            if (commands.Count > 1)
            {
                // Use other file
                string fileNameCopy = Tools.GetNonExistentRandomFileName(Path.GetDirectoryName(fileName), "pmh");
                commands.DumpToFile(fileNameCopy);
                //
                File.Copy(fileNameCopy, fileName, true);
                File.Delete(fileNameCopy);
            }
        }
        // Read
        public void ReadFromFile(string fileName)
        {
            ReadFromFile(fileName, out _commands);
            _currPositionIndex = _commands.Count - 1;
        }
        public static void ReadFromFile(string fileName, out List<Command> commands)
        {
            commands = Tools.LoadDumpFromFile<List<Command>>(fileName);
        }
        // History files
        public string GetHistoryFileNameBin()
        {
            return _historyFileNameBin;
        }
        public void DeleteHistoryFile()
        {
            if (File.Exists(_historyFileNameBin)) File.Delete(_historyFileNameBin);
        }
    }
}
