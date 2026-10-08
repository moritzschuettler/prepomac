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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CaeGlobals;
using System.Diagnostics;
using System.Windows.Forms;
using CaeModel;
using FileInOut.Output;
using FileInOut.Output.Calculix;

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
        private enum KeywordOverrideStatus
        {
            Active,
            Suppressed,
            Conflict
        }
        private class KeywordOverrideState
        {
            public CalculixKeywordOverride KeywordOverride;
        }
        private class KeywordOverridesSnapshot
        {
            public FeModel Model;
            public List<KeywordOverrideState> States;
        }
        
        // Variables                                                                                                                
        private int _currPositionIndex;
        private Controller _controller;
        private List<Command> _commands;
        private string _historyFileNameBin;
        private ViewGeometryModelResults _previousView;
        private List<string> _errors;
        private bool _postprocessingDeclined;
        [NonSerialized] private int _commandRecordingSuppressionCount;
        [NonSerialized] private string _commandBlockReason;


        // Properties                                                                                                               
        public int Count { get { return _commands.Count(); } }
        public int CurrPositionIndex { get { return _currPositionIndex; } }
        public List<Command> Commands { get { return _commands; } }
        // The last command the model was built by, which is the one an undo takes back
        public Command LastExecutedCommand
        {
            get
            {
                if (_currPositionIndex < 0 || _currPositionIndex >= _commands.Count) return null;
                return _commands[_currPositionIndex];
            }
        }
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
            _historyFileNameBin = Path.Combine(Application.StartupPath, Globals.HistoryFileName);
            _previousView = ViewGeometryModelResults.Geometry;
            _errors = null;
            _postprocessingDeclined = false;
            _commandRecordingSuppressionCount = 0;
            //
            WriteToFile();
        }
        public CommandsCollection(Controller controller, CommandsCollection commandsCollection)
            :this(controller)
        {
            if (commandsCollection != null)
            {
                _currPositionIndex = commandsCollection._currPositionIndex;
                _commands = new List<Command>(commandsCollection._commands);
                _previousView = commandsCollection._previousView;
            }
            //
            WriteToFile();
        }


        // Methods                                                                                                                  
        public bool AddAndExecute(Command command)
        {
            RefuseWhenBlocked(command);
            if (_commandRecordingSuppressionCount > 0) return ExecuteCommandWithoutRecording(command);
            //
            return ExecuteCommand(command, true);
        }
        public bool AddAndExecute(Command command, bool executeSynchronous)
        {
            RefuseWhenBlocked(command);
            if (_commandRecordingSuppressionCount > 0)
                return ExecuteCommandWithoutRecording(command, executeSynchronous);
            //
            return ExecuteCommand(command, true, executeSynchronous);
        }
        // Read only mode of the automation server. Every state change passes here, so this is the one
        // place a refusal cannot be worked around by running different Python
        public bool AreCommandsBlocked { get { return _commandBlockReason != null; } }
        public void BlockCommands(string reason)
        {
            if (reason == null) throw new ArgumentNullException("reason");
            _commandBlockReason = reason;
        }
        public void UnblockCommands()
        {
            _commandBlockReason = null;
        }
        public void RefuseWhenBlocked(Command command)
        {
            if (_commandBlockReason == null) return;
            //
            RefuseWhenBlocked(command == null ? "A command" : command.Name);
        }
        public void RefuseWhenBlocked(string actionName)
        {
            if (_commandBlockReason == null) return;
            //
            throw new CaeException(actionName + " was refused. " + _commandBlockReason);
        }
        public bool IsCommandRecordingSuppressed { get { return _commandRecordingSuppressionCount > 0; } }
        public void BeginCommandRecordingSuppression()
        {
            _commandRecordingSuppressionCount++;
        }
        public void EndCommandRecordingSuppression()
        {
            if (_commandRecordingSuppressionCount <= 0)
                throw new InvalidOperationException("Command recording suppression is not active.");
            //
            _commandRecordingSuppressionCount--;
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
                            Clear(false);    // remove all previous commands
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
            // Remove the post-processing commands the user declined to regenerate
            if (_postprocessingDeclined && command is PostprocessCommand) RemoveDeclinedPostprocessingCommands();
            // Remove old commands
            if (_currPositionIndex < _commands.Count - 1)
                _commands.RemoveRange(_currPositionIndex + 1, _commands.Count - _currPositionIndex - 1);
            //
            _commands.Add(command);
        }
        // A command a compatibility pass puts into the history moves the current position with it, the
        // way removing one does, or undo and redo would point at the wrong command
        public void InsertCommand(int index, Command command)
        {
            if (index < 0 || index > _commands.Count) throw new ArgumentOutOfRangeException("index");
            //
            _commands.Insert(index, command);
            if (index <= _currPositionIndex + 1) _currPositionIndex++;
        }
        private bool RemoveCommand(Command command)
        {
            int index = _commands.IndexOf(command);
            if (index < 0) return false;
            //
            _commands.RemoveAt(index);
            if (index <= _currPositionIndex) _currPositionIndex--;
            InvalidateSaveCommands(index);
            //
            return true;
        }
        // A saved file is a state the history builds only while the commands before its save stay as they were
        private void InvalidateSaveCommands(int firstChangedIndex)
        {
            for (int i = Math.Max(firstChangedIndex, 0); i < _commands.Count; i++)
            {
                if (_commands[i] is CSaveToPmx saveToPmx) saveToPmx.Invalidate();
            }
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
            // The edited commands are not compared with the ones the files were saved by
            InvalidateSaveCommands(0);
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
                // There is no need to add open .pmx or open .pmh commands to the list
                if (command is COpenFile cof && (cof.FileExtension == ".pmx" || cof.FileExtension == ".pmh"))
                { }
                else
                {
                    // Add command
                    if (addCommand) AddCommand(command);
                    // Check model changed
                    CheckModelChanged(command);
                    // Write to file
                    Stopwatch historyWriteWatch = Stopwatch.StartNew();
                    WriteToFile();
                    historyWriteWatch.Stop();
                    if (command is CCreateMesh createMesh)
                        createMesh.HistorySerializationTime = historyWriteWatch.Elapsed;
                    //
                    _currPositionIndex++;
                    //
                    OnEnableDisableUndoRedo();
                }
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
        private bool ExecuteCommandWithoutRecording(Command command, bool executeSynchronous = false)
        {
            // Write to form
            WriteToOutput(command);
            //
            bool result = ExecuteCommandWithTimer(command, false, executeSynchronous);
            if (result) CheckModelChanged(command);
            //
            return result;
        }
        private bool ExecuteCommandWithTimer(Command command, bool executeWithDialog = false, bool executeSynchronous = false)
        {
            KeywordOverridesSnapshot keywordOverridesSnapshot = CaptureKeywordOverrides(command);
            bool result = false;
            bool batchKeywordStateNotifications = command is PreprocessCommand;
            List<string> keywordStateMessages = null;
            Stopwatch stopwatch = Stopwatch.StartNew();
            //
            if (batchKeywordStateNotifications) _controller.BeginKeywordStateNotificationBatch();
            // A command executed inside an older one, such as by its Python script, splits the way the older one does
            int numOfAutomaticSplitFaces = _controller.NumOfAutomaticSplitFaces;
            if (command.SplitsPeriodicFacesAutomatically)
                _controller.NumOfAutomaticSplitFaces = _controller.Settings.General.NumOfSplitFaces;
            try
            {
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
            }
            finally
            {
                _controller.NumOfAutomaticSplitFaces = numOfAutomaticSplitFaces;
                if (batchKeywordStateNotifications)
                    keywordStateMessages = _controller.EndKeywordStateNotificationBatch();
            }
            //
            stopwatch.Stop();
            command.TimeSpan = stopwatch.Elapsed;
            //
            if (result) ReconcileKeywordOverrides(command, keywordOverridesSnapshot, keywordStateMessages);
            else _controller.ShowOrDeferKeywordStateMessages(keywordStateMessages);
            //
            return result;
        }
        private KeywordOverridesSnapshot CaptureKeywordOverrides(Command command)
        {
            if (!(command is PreprocessCommand) || command is CSetCalculixKeywordEditorState) return null;
            FeModel model = _controller.Model;
            if (model == null || model.CalculixKeywordOverrides == null ||
                model.CalculixKeywordOverrides.Count == 0)
                return null;
            //
            try
            {
                KeywordOverridesSnapshot snapshot = new KeywordOverridesSnapshot();
                snapshot.Model = model;
                snapshot.States = new List<KeywordOverrideState>();
                foreach (CalculixKeywordOverride keywordOverride in model.CalculixKeywordOverrides)
                {
                    if (keywordOverride == null) continue;
                    snapshot.States.Add(new KeywordOverrideState
                    {
                        KeywordOverride = keywordOverride
                    });
                }
                return snapshot;
            }
            catch
            {
                // Failure to inspect generated keywords must not block the GUI command.
                return null;
            }
        }
        private void ReconcileKeywordOverrides(Command command, KeywordOverridesSnapshot snapshot,
                                               List<string> keywordStateMessages)
        {
            List<string> parts = keywordStateMessages ?? new List<string>();
            if (snapshot != null && ReferenceEquals(_controller.Model, snapshot.Model))
            {
                try
                {
                    List<CalculixKeywordOverride> keywordOverrides = snapshot.Model.CalculixKeywordOverrides;
                    List<CalculixKeyword> keywords = CalculixFileWriter.GetModelKeywords(
                        snapshot.Model, _controller.Settings.Solver.ConvertPyramidsTo, null, true);
                    int suppressedCount = 0;
                    int unsuppressedCount = 0;
                    int removedCount = 0;
                    foreach (KeywordOverrideState state in snapshot.States)
                    {
                        CalculixKeywordOverride currentOverride = keywordOverrides.FirstOrDefault(
                            item => ReferenceEquals(item, state.KeywordOverride) ||
                                    item != null && item.HasSameIndices(state.KeywordOverride.Indices) &&
                                    item.OriginalSignature == state.KeywordOverride.OriginalSignature);
                        if (currentOverride == null) continue;
                        //
                        KeywordOverrideStatus status = GetKeywordOverrideStatus(keywords, currentOverride);
                        if (status == KeywordOverrideStatus.Active)
                        {
                            if (currentOverride.IsSuppressed)
                            {
                                currentOverride.Unsuppress();
                                unsuppressedCount++;
                            }
                        }
                        else if (status == KeywordOverrideStatus.Suppressed)
                        {
                            if (!currentOverride.IsSuppressed)
                            {
                                currentOverride.Suppress();
                                suppressedCount++;
                            }
                        }
                        else
                        {
                            keywordOverrides.Remove(currentOverride);
                            removedCount++;
                        }
                    }
                    //
                    if (suppressedCount > 0 || unsuppressedCount > 0 || removedCount > 0)
                    {
                        _controller.UpdateNumberOfModelModifiedKeywords();
                        if (suppressedCount > 0)
                            parts.Add("Number of suppressed modified CalculiX keywords: " + suppressedCount + ".");
                        if (unsuppressedCount > 0)
                            parts.Add("Number of unsuppressed modified CalculiX keywords: " + unsuppressedCount + ".");
                        if (removedCount > 0)
                        {
                            string suffix = removedCount == 1 ? " was" : "s were";
                            parts.Add(removedCount + " modified CalculiX keyword" + suffix +
                                      " reset because the feature '" + command.Name + "' changed it.");
                        }
                    }
                }
                catch
                {
                    // Keep all overrides if regenerated keyword inspection fails.
                }
            }
            //
            _controller.ShowOrDeferKeywordStateMessages(parts);
        }
        private static KeywordOverrideStatus GetKeywordOverrideStatus(
            List<CalculixKeyword> keywords, CalculixKeywordOverride keywordOverride)
        {
            if (keywords == null || keywordOverride == null) return KeywordOverrideStatus.Conflict;
            int[] indices = keywordOverride.Indices;
            if (indices == null || indices.Length == 0) return KeywordOverrideStatus.Conflict;
            //
            List<CalculixKeyword> currentKeywords = keywords;
            CalculixKeyword keyword = null;
            foreach (int index in indices)
            {
                if (index < 0 || index >= currentKeywords.Count) return KeywordOverrideStatus.Conflict;
                keyword = currentKeywords[index];
                if (keyword is CalDeactivated) return KeywordOverrideStatus.Suppressed;
                currentKeywords = keyword.Keywords;
            }
            //
            string signature = CalculixFileWriter.GetKeywordSignature(keyword);
            if (signature == keywordOverride.OriginalSignature) return KeywordOverrideStatus.Active;
            else return KeywordOverrideStatus.Conflict;
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
            string oldFileName;
            Dictionary<string, string> replacedFileNames = new Dictionary<string, string>();
            _errors = new List<string>();
            //
            foreach (Command command in _commands)    // clear command modifies the collection
            {
                if (count++ <= _currPositionIndex)
                {
                    // Set working directory while in regeneration mode
                    if (_controller.BatchRegenerationMode) SetFileNamesToWorkDirectory(command);
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
                                                 showMeshDialog && cwd is CReplaceMeshSetupItem ||
                                                 showFileDialog && cwd is CImportParameters ||
                                                 showFileDialog && cwd is IFileDependentCommand);
                            // Ask only once for each file - reuse the replacement for all commands using the same file
                            oldFileName = executeWithDialog ? GetInputFileName(command) : null;
                            if (oldFileName != null && replacedFileNames.TryGetValue(oldFileName, out string newFileName))
                            {
                                if (newFileName != oldFileName) SetInputFileName(command, newFileName);
                                executeWithDialog = false;
                            }
                            //
                            ExecuteCommandWithTimer(command, executeWithDialog, true);
                            //
                            if (executeWithDialog && oldFileName != null)
                                replacedFileNames[oldFileName] = GetInputFileName(command);
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
        private void SetFileNamesToWorkDirectory(Command command)
        {
            string newFileName;
            string workDirectory = _controller.Settings.GetWorkDirectory();
            string inputFileName = GetInputFileName(command);
            // Input files are used from the work directory only if they exist there
            if (inputFileName != null)
            {
                newFileName = Path.Combine(workDirectory, Path.GetFileName(inputFileName));
                if (File.Exists(newFileName))
                {
                    // Some model items read the file when the file name is set - the error is reported at execution
                    try { SetInputFileName(command, newFileName); }
                    catch { }
                }
            }
            else if (command is CSaveToPmx cSaveToPmx)
            {
                newFileName = Path.Combine(workDirectory, Path.GetFileName(cSaveToPmx.FileName));
                cSaveToPmx.FileName = newFileName;
            }
            else if (command is IExportFileCommand exportFileCommand)
            {
                newFileName = Path.Combine(workDirectory, Path.GetFileName(exportFileCommand.FileName));
                exportFileCommand.FileName = newFileName;
            }
        }
        private string GetInputFileName(Command command)
        {
            if (command is COpenFile cOpenFile) return cOpenFile.FileName;
            else if (command is IImportFileCommand importFileCommand) return importFileCommand.FileName;
            else if (command is IFileDependentCommand fileDependentCommand) return fileDependentCommand.FileName;
            else return null;
        }
        private void SetInputFileName(Command command, string fileName)
        {
            if (command is COpenFile cOpenFile) cOpenFile.FileName = fileName;
            else if (command is IImportFileCommand importFileCommand) importFileCommand.FileName = fileName;
            else if (command is IFileDependentCommand fileDependentCommand) fileDependentCommand.FileName = fileName;
        }
        // The last save the history up to the current position can be replayed from: its file is unchanged and holds
        // what the replay would build, which for all commands includes the results
        public CSaveToPmx GetLastSaveCommand(RegenerateTypeEnum regenerateType)
        {
            int lastCommandIndex = Math.Min(_currPositionIndex, _commands.Count - 1);
            HashSet<string> checkedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            //
            for (int i = lastCommandIndex; i >= 0; i--)
            {
                if (_commands[i] is CSaveToPmx saveToPmx)
                {
                    if (regenerateType != RegenerateTypeEnum.PreProcess && !saveToPmx.ResultsSaved &&
                        ContainsNonPreprocessCommands(i))
                        continue;
                    // Only the last save of a file can match it
                    if (!checkedFileNames.Add(Tools.GetGlobalPath(saveToPmx.FileName))) continue;
                    if (saveToPmx.IsFileHashUnchanged()) return saveToPmx;
                }
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
        public bool IsRegenerateTypeUsed()
        {
            return ContainsNonPreprocessCommands(_currPositionIndex);
        }
        public bool IsRegenerateTypeUsedByUndo()
        {
            return IsUndoPossible && !IsGeneralCommandAffected() && ContainsNonPreprocessCommands(_currPositionIndex - 1);
        }
        public static bool IsRegenerateTypeUsedByFile(string fileName)
        {
            List<Command> commands;
            ReadFromFile(fileName, out commands);
            return ContainsNonPreprocessCommands(commands, commands.Count - 1);
        }
        private bool ContainsNonPreprocessCommands(int lastCommandIndex)
        {
            return ContainsNonPreprocessCommands(_commands, lastCommandIndex);
        }
        private static bool ContainsNonPreprocessCommands(List<Command> commands, int lastCommandIndex)
        {
            if (lastCommandIndex > commands.Count - 1) lastCommandIndex = commands.Count - 1;
            //
            for (int i = 0; i <= lastCommandIndex; i++)
            {
                if (!(commands[i] is PreprocessCommand) && !(commands[i] is SaveCommand)) return true;
            }
            return false;
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
        public int GetNumberOfPostprocessCommands()
        {
            List<Command> reducedCommands = GetReducedPostprocessingCommands();
            return reducedCommands.Count();
        }
        public bool RunHistoryPostprocessing()
        {
            _postprocessingDeclined = false;
            //
            List<Command> reducedCommands = GetReducedPostprocessingCommands(true);
            // Run commands
            return AddAndExecute(reducedCommands);
        }
        public void SetPostprocessingDeclined(bool postprocessingDeclined)
        {
            _postprocessingDeclined = postprocessingDeclined;
        }
        private void RemoveDeclinedPostprocessingCommands()
        {
            _postprocessingDeclined = false;
            //
            GetReducedPostprocessingCommands(true);
        }
        public List<Command> GetReducedPostprocessingCommands(bool removeUnnecessaryCommands = false)
        {
            bool commandsRemoved = false;
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
                    if (currentAnalysisName != null) // null is for post processing commands done of preview of loads, ...
                    {
                        if (command is CRemoveCurrentResults) { }   // skipp
                        else if (command is CRemoveAllResults) { }  // skipp
                        else
                        {
                            if (postprocessCommands.TryGetValue(currentAnalysisName, out commandsList)) commandsList.Add(command);
                            else postprocessCommands.Add(currentAnalysisName, new List<Command> { command });
                        }
                    }
                }
            }
            // Run postprocessing commands for the last opened analysis
            List<Command> runCommandsList;
            List<Command> postprocessCommandsList;
            List<Command> reducedCommands = new List<Command>();
            //
            if (lastAnalysisName != null && postprocessCommands.TryGetValue(lastAnalysisName, out postprocessCommandsList) &&
                runCommands.TryGetValue(lastAnalysisName, out runCommandsList))
            {
                int count = 0;
                COpenResults lastOpenResultsCommand = null;
                // Remove previous/unnecessary run commands in commands history
                foreach (var command in runCommandsList)
                {
                    if (removeUnnecessaryCommands && count++ < runCommandsList.Count - 1)
                        commandsRemoved |= RemoveCommand(command);
                }
                // Find last open results command
                foreach (var command in postprocessCommandsList)
                {
                    if (command is COpenResults or) lastOpenResultsCommand = or;
                }
                // Remove all postprocess commands except the last open result command from the commands history
                foreach (var command in postprocessCommandsList)
                {
                    if (removeUnnecessaryCommands && command != lastOpenResultsCommand)
                        commandsRemoved |= RemoveCommand(command);
                }
                // Add necessary commands
                foreach (var command in postprocessCommandsList)
                {
                    if (command is COpenResults) { }
                    else if (command is CSetCurrentResults) { }
                    else reducedCommands.Add(command);
                }
            }
            // Update the history after the command removal
            if (commandsRemoved)
            {
                WriteToFile();
                OnEnableDisableUndoRedo();
            }
            // Return commands
            return reducedCommands;
        }

        // Clear
        public void Clear(bool setErrorsToNull = true)
        {
            _currPositionIndex = -1;
            _commands.Clear();
            _previousView = ViewGeometryModelResults.Geometry;
            if (setErrorsToNull) _errors = null;
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
        // The step back of an undo, after which the caller replays the history up to the new position
        public bool MoveBackForUndo(ref RegenerateTypeEnum regenerateType)
        {
            if (!IsUndoPossible) return false;
            //
            if (IsGeneralCommandAffected()) regenerateType = RegenerateTypeEnum.All;
            //
            _currPositionIndex--;
            //
            return true;
        }
        // The same step back as an undo, for a caller that has already put the model back to the state it had
        // before the last command, so the history is not replayed. The command stays for a redo.
        public void UndoWithoutRegeneration()
        {
            if (!IsUndoPossible) return;
            //
            _currPositionIndex--;
            _controller.ModelChanged = true;
            //
            OnEnableDisableUndoRedo();
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
        private bool IsGeneralCommandAffected()
        {
            int lastCommandIndex = Math.Min(_currPositionIndex, _commands.Count - 1);
            //
            for (int i = 0; i <= lastCommandIndex; i++)
            {
                if (_commands[i] is GeneralCommand) return true;
            }
            return false;
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
            // Use other file
            string fileNameCopy = Tools.GetNonExistentRandomFileName(Path.GetDirectoryName(fileName), "pmh");
            commands.DumpToFile(fileNameCopy);
            //
            File.Copy(fileNameCopy, fileName, true);
            File.Delete(fileNameCopy);
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
