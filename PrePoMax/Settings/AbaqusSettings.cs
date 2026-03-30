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
using System.ComponentModel;
using CaeGlobals;
using System.IO;
using CaeJob;
using FileInOut.Output.Calculix;
using DynamicTypeDescriptor;
using CaeModel;
using System.Windows.Forms;

namespace PrePoMax
{
   [Serializable]
    public class AbaqusSettings : ISettings
    {
        // Variables                                                                                                                
        private string _workDirectory;
        private bool _usePmxFolderAsWorkDirectory;
        private string _executable;
        private int _numCPUs;
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
        public string AbaqusExe 
        {
            get { return Tools.GetGlobalPath(_executable); }
            set
            {
                string path = Tools.GetGlobalPath(value);
                if (!File.Exists(path))
                    throw new Exception("The selected Abaqus executable does not exist.");
                if (Path.GetExtension(path) != ".exe" && Path.GetExtension(path) != ".bat" && Path.GetExtension(path) != ".cmd")
                    throw new Exception("The selected executable's file extension is not .exe, .bat or .cmd.");
                _executable = Tools.GetLocalPath(path);
            }
        }
        public int NumCPUs
        {
            get { return _numCPUs; }
            set
            {
                _numCPUs = value;
                if (_numCPUs < 1) _numCPUs = 1;
            }
        }


        // Constructors                                                                                                             
        public AbaqusSettings()
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
            _executable = "";
            _numCPUs = 1;
        }
    }
}
