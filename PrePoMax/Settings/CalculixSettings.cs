using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using System.IO;
using CaeJob;
using FileInOut.Output.Calculix;
using DynamicTypeDescriptor;
using CaeModel;

namespace PrePoMax
{
   [Serializable]
    public class CalculixSettings : ISettings
    {
        // Variables                                                                                                                
        private string _workDirectory;
        private bool _usePmxFolderAsWorkDirectory;
        private string _executable;
        private SolverTypeEnum _solverTypeEnum;
        private int _numCPUs;
        private List<EnvironmentVariable> _environmentVariables;
        private ConvertPyramidsToEnum _convertPyramidsTo;

        // Properties                                                                                                               
        public string WorkDirectoryForSettingsOnly
        { 
            get { return Tools.GetGlobalPath(_workDirectory); }
            set 
            {
                string path = Tools.GetGlobalPath(value);
                _workDirectory = Tools.GetLocalPath(path);
            } 
        }
        public bool UsePmxFolderAsWorkDirectory
        {
            get { return _usePmxFolderAsWorkDirectory; }
            set { _usePmxFolderAsWorkDirectory = value; }
        }
        public string CalculixExe 
        {
            get { return Tools.GetGlobalPath(_executable); }
            set
            {
                string path = Tools.GetGlobalPath(value);
                if (!File.Exists(path))
                    throw new Exception("The selected calculix executable does not exist.");
                if (Path.GetExtension(path) != ".exe" && Path.GetExtension(path) != ".bat" && Path.GetExtension(path) != ".cmd")
                    throw new Exception("The selected executable's file extension is not '.exe', '.bat' or '.cmd'.");
                _executable = Tools.GetLocalPath(path);
            }
        }
        public CaeModel.SolverTypeEnum DefaultSolverType { get { return _solverTypeEnum; } set { _solverTypeEnum = value; } }
        public int NumCPUs
        {
            get { return _numCPUs; }
            set
            {
                _numCPUs = value;
                if (_numCPUs < 1) _numCPUs = 1;
            }
        }
        public List<EnvironmentVariable> EnvironmentVariables
        {
            get { return _environmentVariables; }
            set { _environmentVariables = value; }
        }
        public ConvertPyramidsToEnum ConvertPyramidsTo { get { return _convertPyramidsTo; } set { _convertPyramidsTo = value; } }


        // Constructors                                                                                                             
        public CalculixSettings()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public void CheckValues()
        {
        }
        public void Reset()
        {
            _workDirectory = null;
            _usePmxFolderAsWorkDirectory = false;
            _executable = null;
            _solverTypeEnum = SolverTypeEnum.Pardiso;
            _numCPUs = 1;
            _environmentVariables = null;
            _convertPyramidsTo = ConvertPyramidsToEnum.Wedges;
        }
    }
}
