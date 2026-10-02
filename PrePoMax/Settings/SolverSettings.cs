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
using CaeGlobals;
using CaeJob;
using CaeMesh;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class SolverSettings : ISettings
    {
        // Variables
        private string _workDirectory;
        private bool _usePmxFolderAsWorkDirectory;
        private int _numCPUs;
        private string _calculixExe;
        private SolverTypeEnum _calculixDefaultSolverType;
        private List<EnvironmentVariable> _environmentVariables;
        private ConvertPyramidsToEnum _convertPyramidsTo;
        private bool _binaryResultsFile;
        private string _abaqusExe;
        private string _femasterExe;
        public static string NonEnglishDirectoryWarning = "The selected work directory path contains non-English characters. " +
                                                          "Some program features might not work as expected.";


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
        public int NumCPUs
        {
            get { return _numCPUs; }
            set { _numCPUs = Math.Max(1, value); }
        }
        public string CalculixExe
        {
            get { return Tools.GetGlobalPath(_calculixExe); }
            set { _calculixExe = SetExecutable(value, "CalculiX"); }
        }
        public SolverTypeEnum CalculixDefaultSolverType
        {
            get { return _calculixDefaultSolverType; }
            set { _calculixDefaultSolverType = value; }
        }
        public List<EnvironmentVariable> EnvironmentVariables
        {
            get { return _environmentVariables; }
            set { _environmentVariables = value; }
        }
        public ConvertPyramidsToEnum ConvertPyramidsTo
        {
            get { return _convertPyramidsTo; }
            set { _convertPyramidsTo = value; }
        }
        public bool BinaryResultsFile
        {
            get { return _binaryResultsFile; }
            set { _binaryResultsFile = value; }
        }
        public string AbaqusExe
        {
            get { return Tools.GetGlobalPath(_abaqusExe); }
            set { _abaqusExe = SetExecutable(value, "Abaqus"); }
        }
        public string FEMasterExe
        {
            get { return Tools.GetGlobalPath(_femasterExe); }
            set { _femasterExe = SetExecutable(value, "FEMaster"); }
        }


        // Constructors
        public SolverSettings()
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
            _numCPUs = 1;
            _calculixExe = null;
            _calculixDefaultSolverType = SolverTypeEnum.Pardiso;
            _environmentVariables = null;
            _convertPyramidsTo = ConvertPyramidsToEnum.Wedges;
            _binaryResultsFile = false;
            _abaqusExe = null;
            _femasterExe = null;
        }
        private static string SetExecutable(string value, string solverName)
        {
            string path = Tools.GetGlobalPath(value);
            if (!File.Exists(path))
                throw new Exception("The selected " + solverName + " executable does not exist.");
            string extension = Path.GetExtension(path);
            if (extension != ".exe" && extension != ".bat" && extension != ".cmd")
                throw new Exception("The selected executable's file extension is not .exe, .bat or .cmd.");
            return Tools.GetLocalPath(path);
        }
    }
}
