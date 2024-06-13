using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CaeGlobals;

namespace PrePoMax.Commands
{
    [Serializable]
    public class CommandsCollection
    {
        
        // Variables                                                                                                                
        protected bool _showDialogs;
        protected int _currPositionIndex;
        protected Controller _controller;
        protected List<Command> _commands;
        protected string _historyFileNameBin;
        protected ViewGeometryModelResults _previousView;


        // Properties                                                                                                               
        public int Count { get { return _commands.Count(); } }
        public int CurrPositionIndex { get { return _currPositionIndex; } }
        public List<Command> Commands { get { return _commands; } }


        // Callbacks                                                                                                                
        [NonSerialized] public Action<string> WriteOutput;
        [NonSerialized] public Action ModelChanged_ResetJobStatus;


        // Events                                                                                                                   
        public event Action<string, string> EnableDisableUndoRedo;


        // Constructor                                                                                                              
        public CommandsCollection(Controller controller)
        {
            _controller = controller;
            _currPositionIndex = -1;
            _commands = new List<Command>();
            _historyFileNameBin = Path.Combine(System.Windows.Forms.Application.StartupPath, Globals.HistoryFileName + ".pmh");
            _previousView = ViewGeometryModelResults.Geometry;
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
        public void AddAndExecute(Command command)
        {
            ExecuteCommand(command, true);
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
            //
            if (command is CSaveToPmx) { }
            else if (command is PreprocessCommand)
            {
                _controller.ModelChanged = true;
                ModelChanged_ResetJobStatus?.Invoke();
            }
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
            ModelChanged_ResetJobStatus?.Invoke();
        }
        private void ExecuteCommand(Command command, bool addCommand)
        {
            // Write to form
            WriteToOutput(command);
            // First execute to check for errors - DO NOT EXECUTE SAVE COMMAND
            if (command is CSaveToPmx || command.Execute(_controller))
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
            }
            // Execute the save command at the end to include all changes in the file
            if (command is CSaveToPmx)
            {
                command.Execute(_controller);
                WriteToFile();  // repeat the write in order to save the hash
            }
        }
        public void ExecuteAllCommandsFromLastSave()
        {
            ExecuteAllCommands();
        }
        public void ExecuteAllCommands()
        {
            ExecuteAllCommands(false, false, null);
        }
        public void ExecuteAllCommands(bool showImportDialog, bool showMeshDialog)
        {
            ExecuteAllCommands(showImportDialog, showMeshDialog, null);
        }
        public void ExecuteAllCommandsFromLastSave(CSaveToPmx lastSave)
        {
            ExecuteAllCommands(false, false, lastSave);
        }
        public void ExecuteAllCommands(bool showImportDialog, bool showMeshDialog, CSaveToPmx lastSave)
        {
            int count = 0;
            bool executeWithDialog;
            List<string> errors = new List<string>();
            //
            foreach (Command command in _commands)
            {
                if (!(command is PreprocessCommand))
                    continue;
                //
                if (count++ <= _currPositionIndex)
                {
                    // Write to form
                    WriteToOutput(command);
                    // Try
                    try
                    {
                        // Skip all up to last save
                        if (lastSave != null && command != lastSave) { }
                        // Skip save
                        else if (command is CSaveToPmx)
                        {
                            if (lastSave != null && command == lastSave) lastSave = null;
                        }
                        // Execute
                        else
                        {
                            executeWithDialog = false;
                            if (command is ICommandWithDialog cwd)
                            {
                                if (showImportDialog && cwd is CImportFile) executeWithDialog = true;
                                //
                                else if (showMeshDialog && cwd is CAddMeshingParameters) executeWithDialog = true;
                                else if (showMeshDialog && cwd is CAddMeshRefinement) executeWithDialog = true;
                                else if (showMeshDialog && cwd is CAddMeshSetupItem) executeWithDialog = true;
                                //
                                else if (showMeshDialog && cwd is CReplaceMeshingParameters) executeWithDialog = true;
                                else if (showMeshDialog && cwd is CReplaceMeshRefinement) executeWithDialog = true;
                                else if (showMeshDialog && cwd is CReplaceMeshSetupItem) executeWithDialog = true;
                                //
                                if (executeWithDialog) cwd.ExecuteWithDialog(_controller);
                            }
                            // Execute without dialog
                            if (!executeWithDialog) command.Execute(_controller);
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(command.Name + ": " + ex.Message);
                    }
                    // Check model changed
                    CheckModelChanged(command);
                }
                else break;
            }
            // Report Errors
            if (errors.Count != 0)
            {
                WriteOutput?.Invoke("");
                WriteOutput?.Invoke("****   Exceptions   ****");                
                foreach (var error in errors)
                {
                    WriteOutput?.Invoke(error);
                }
                WriteOutput?.Invoke("****   Number of exceptions: " + errors.Count + "   ****");
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
        // Clear
        public void Clear()
        {
            _currPositionIndex = -1;
            _commands.Clear();
            _previousView = ViewGeometryModelResults.Geometry;
            // Write to file
            WriteToFile();
            //
            OnEnableDisableUndoRedo();
            //
            ModelChanged_ResetJobStatus?.Invoke();
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
        public void Undo()
        {
            if (IsUndoPossible)
            {
                _currPositionIndex--;
                ExecuteAllCommands();   // also rewrites history
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
            if (_commands.Count > 1)
            {
                // Write to files
                _commands.DumpToFile(_historyFileNameBin);
                // Use other file
                string fileName = Tools.GetNonExistentRandomFileName(System.Windows.Forms.Application.StartupPath, "pmh");
                _commands.DumpToFile(fileName);
                //
                File.Copy(fileName, _historyFileNameBin, true);
                File.Delete(fileName);
            }
        }
        public void ReadFromFile(string fileName)
        {
            _commands = Tools.LoadDumpFromFile<List<Command>>(fileName);
            _currPositionIndex = _commands.Count - 1;
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
