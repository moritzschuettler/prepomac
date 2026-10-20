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
            get
            {
                if (_workDirectory == null && Environment.OSVersion.Platform != PlatformID.Win32NT)
                {
                    // A folder in the home folder; the Documents folder would ask for a permission
                    string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                               "PrePoMax", "Work");
                    try { Directory.CreateDirectory(path); _workDirectory = path; }
                    catch { }
                }
                return Tools.GetGlobalPath(_workDirectory);
            }
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
            get
            {
                // Settings saved before a ccx was installed
                if (_calculixExe == null && Environment.OSVersion.Platform != PlatformID.Win32NT)
                    _calculixExe = FindCalculixExecutable();
                return Tools.GetGlobalPath(_calculixExe);
            }
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
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                // The CalculiX builds for macOS link only the SPOOLES solver; an installed ccx is used by default
                _calculixExe = FindCalculixExecutable();
                _calculixDefaultSolverType = SolverTypeEnum.Spooles;
            }
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
            // The executables of macOS and Linux have any name, such as ccx_2.23
            if (Environment.OSVersion.Platform == PlatformID.Win32NT && extension != ".exe" && extension != ".bat" && extension != ".cmd")
                throw new Exception("The selected executable's file extension is not .exe, .bat or .cmd.");
            return Tools.GetLocalPath(path);
        }
        private static string FindCalculixExecutable()
        {
            List<string> directories = new List<string>
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Solver"),
                "/opt/homebrew/bin",
                "/usr/local/bin",
            };
            string pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (pathVariable != null) directories.AddRange(pathVariable.Split(Path.PathSeparator));
            foreach (string directory in directories)
            {
                try
                {
                    if (!Directory.Exists(directory)) continue;
                    string ccx = Path.Combine(directory, "ccx");
                    if (File.Exists(ccx)) return ccx;
                    // Versioned names such as ccx_2.23; the newest version first
                    string[] files = Directory.GetFiles(directory, "ccx_*");
                    Array.Sort(files, StringComparer.Ordinal);
                    for (int i = files.Length - 1; i >= 0; i--)
                    {
                        string name = Path.GetFileName(files[i]);
                        if (name.Length > 4 && char.IsDigit(name[4])) return files[i];
                    }
                }
                catch { }
            }
            return null;
        }
    }
}
